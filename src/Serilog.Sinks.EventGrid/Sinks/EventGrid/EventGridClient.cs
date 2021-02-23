﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog.Events;

namespace Serilog.Sinks.EventGrid
{
    public class EventGridClient
    {
        private readonly string _key;
        private readonly Uri _topicUri;
        private readonly string _customEventSubject;
        private readonly string _customEventType;
        private readonly CustomEventRequestAuth _customEventRequestAuth;
        private readonly string _customSubjectPropertyName;
        private readonly string _customTypePropertyName;

        public EventGridClient(string key,
          Uri topicUri,
          string customEventSubject,
          string customEventType,
          CustomEventRequestAuth customEventRequestAuth,
          string customSubjectPropertyName,
          string customTypePropertyName)
        {
            _key = key;
            _topicUri = topicUri;
            _customEventSubject = customEventSubject;
            _customEventType = customEventType;
            _customEventRequestAuth = customEventRequestAuth;
            _customSubjectPropertyName = customSubjectPropertyName;
            _customTypePropertyName = customTypePropertyName;
        }

        public async Task SendEvent(LogEvent logEvent)
        {
            // make a dictionary from the log event properties
            var props = logEvent.Properties
              .Select(pv => new { Name = pv.Key, Value = EventGridPropertyFormatter.Simplify(pv.Value) })
              .ToDictionary(a => a.Name, b => b.Value);

            // build the request from sink config and log event properties
            var customEvent = new CustomEventRequest
            {
                Subject = _customEventSubject ?? GetEventSubjectFromProperties(props),
                EventType = _customEventType ?? GetEventTypeFromProperties(props)
            };

            // if we don't have what we need from the config or the event, pull event info from the call stack
            if (string.IsNullOrEmpty(customEvent.Subject) || string.IsNullOrEmpty(customEvent.EventType))
                GetEventInfoFromAttribute(customEvent);

            // add log severity information
            props.Add("Severity", logEvent.Level.ToString());

            // clean up the payload
            props.Add("LogMessage", logEvent.MessageTemplate.Render(logEvent.Properties));
            customEvent.Data = props.Where(p => p.Key != _customSubjectPropertyName && p.Key != _customTypePropertyName);

            // finally, we have what we need post the event
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _topicUri);
            request.Headers.Add(_customEventRequestAuth == CustomEventRequestAuth.Key ? "aeg-sas-key" : "aeg-sas-token", _key);
            var body = new[] { customEvent };

            var json = JsonConvert.SerializeObject(body, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            request.Content = new StringContent(json);
            await client.SendAsync(request);
        }

        private string GetEventSubjectFromProperties(Dictionary<string, object> props)
        {
            return props.ContainsKey(_customSubjectPropertyName) ? props.First(p => p.Key == _customSubjectPropertyName).Value.ToString() : null;
        }

        private string GetEventTypeFromProperties(Dictionary<string, object> props)
        {
            return props.ContainsKey(_customTypePropertyName) ? props.First(p => p.Key == _customTypePropertyName).Value.ToString() : null;
        }

        private void GetEventInfoFromAttribute(CustomEventRequest customEvent)
        {
            // walk up the stack and check for our EventGridSinkAttribute
            var st = new StackTrace();
            var stackFrames = st.GetFrames();
            if (stackFrames == null) return;

            var methods = stackFrames.Where(f => f != null).Select(f => f.GetMethod()).ToArray();
            // walk through serilog to reach the calling method
            var callingMethod = methods.FirstOrDefault(m => !m?.DeclaringType?.FullName?.StartsWith("Serilog") ?? false) ?? methods.First();

            var subjectAttributeValue = GetCustomValueFromAttribute<EventGridSubjectAttribute>(methods);
            var typeAttributeValue = GetCustomValueFromAttribute<EventGridTypeAttribute>(methods);

            // assign the event info, failing back to generic defaults
            customEvent.Subject = customEvent.Subject ?? subjectAttributeValue ?? GetSubject();
            customEvent.EventType = customEvent.EventType ?? typeAttributeValue ?? _customEventType ?? GetEventType() ?? "AppDomain/Class";

            string GetSubject()
            {
                var methodName = GetMethodWithParams() ?? "Method/default";
                return $"{methodName}";
            }

            string GetMethodWithParams()
            {
                var parameterNames = callingMethod != null && callingMethod.GetParameters().Any()
                  ? callingMethod.GetParameters().Select(x => x.Name).Aggregate((combined, next) => combined += string.IsNullOrEmpty(combined) ? next : $"/{next}")
                  : "default";
                var methodWithParameters = $"{callingMethod?.Name}/{parameterNames}";
                return methodWithParameters;
            }

            string GetEventType()
            {
                var assemblyName = callingMethod?.ReflectedType?.Assembly.GetName().Name ?? "General";
                var className = callingMethod?.ReflectedType?.Name ?? "Class";
                return $"{assemblyName}/{className}";
            }
        }

        private static string GetCustomValueFromAttribute<TAttribute>(MethodBase[] methods) where TAttribute : Attribute, IEventGridAttribute
        {
            TAttribute tAttribute;
            // look for the first method in the stack with the type attribute
            var methodAttribute = methods.FirstOrDefault(m => m.GetCustomAttribute<TAttribute>() != null)?.GetCustomAttribute<TAttribute>();
            if (methodAttribute != null)
                tAttribute = methodAttribute;
            else
            {
                // then look for the first class with the attribute, there can be only one
                var classAttribute = methods.FirstOrDefault(m => m.ReflectedType != null && m.ReflectedType.GetCustomAttribute<TAttribute>() != null)?.ReflectedType?.GetCustomAttribute<TAttribute>();
                tAttribute = classAttribute;
            }
            return tAttribute?.CustomValue;
        }
    }
}

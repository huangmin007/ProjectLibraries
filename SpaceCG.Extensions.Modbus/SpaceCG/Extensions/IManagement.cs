using System;
using System.Collections.Generic;
using System.Xml.Linq;
using SpaceCG.Generic;

namespace SpaceCG.Extensions
{
    internal interface IManagement
    {
        void Configuration(ReflectionInterface reflectionInterface, string managerName);

        void TryParseElements(IEnumerable<XElement> elements);

        void CallEventType(string eventType, string parentName);

        void CallEventName(string eventName);

        void CallEventName(string eventType, string parentName);

        void RemoveAll();
    }
}

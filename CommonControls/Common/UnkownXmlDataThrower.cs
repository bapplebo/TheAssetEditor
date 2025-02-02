﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CommonControls.Common
{
    public class UnkownXmlDataThrower
    {
        public XmlDeserializationEvents EventHandler { get; set; }
        public UnkownXmlDataThrower()
        {
            EventHandler = new XmlDeserializationEvents()
            {
                OnUnknownAttribute = Attribute,
                OnUnknownNode = Node,
                OnUnknownElement = Element,
            };
        }

        void Attribute(object sender, XmlAttributeEventArgs e)
        {
            throw new XmlException("Unsuported xml attribute : " + e.Attr.LocalName + $" at line {e.LineNumber} and position {e.LinePosition}", null, e.LineNumber, e.LinePosition);
        }

        void Node(object sender, XmlNodeEventArgs e)
        {
            throw new XmlException("Unsuported xml node : " + e.LocalName + $" at line {e.LineNumber} and position {e.LinePosition}", null, e.LineNumber, e.LinePosition);
        }

        void Element(object sender, XmlElementEventArgs e)
        {
            throw new XmlException("Unsuported xml element : " + e.Element.LocalName + $" at line {e.LineNumber} and position {e.LinePosition}", null, e.LineNumber, e.LinePosition);
        }
    }
}

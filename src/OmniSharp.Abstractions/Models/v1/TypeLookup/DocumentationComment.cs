﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace OmniSharp.Models.TypeLookup
{
    public class DocumentationComment
    {
        public string SummaryText { get; }
        public DocumentationItem[] TypeParamElements { get; }
        public DocumentationItem[] ParamElements { get; }
        public string ReturnsText { get; }
        public string RemarksText { get; }
        public string ExampleText { get; }
        public string ValueText { get; }
        public DocumentationItem[] Exception { get; }

        private DocumentationComment(string summaryText, DocumentationItem[] typeParamElements, DocumentationItem[] paramElements, string returnsText, string remarksText, string exampleText, string valueText, DocumentationItem[ ] exception)
        {
            SummaryText = summaryText;
            TypeParamElements = typeParamElements;
            ParamElements = paramElements;
            ReturnsText = returnsText;
            RemarksText = remarksText;
            ExampleText = exampleText;
            ValueText = valueText;
            Exception = exception;
        }

        private DocumentationComment()
        {
            SummaryText = string.Empty;
            TypeParamElements = Array.Empty<DocumentationItem>();
            ParamElements = Array.Empty<DocumentationItem>(); ;
            ReturnsText = string.Empty;
            RemarksText = string.Empty;
            ExampleText = string.Empty;
            ValueText = string.Empty;
            Exception = Array.Empty<DocumentationItem>();
        }

        public static DocumentationComment From(string xmlDocumentation, string lineEnding)
        {
            if (string.IsNullOrEmpty(xmlDocumentation))
                return Empty;
            var reader = new StringReader("<docroot>" + xmlDocumentation + "</docroot>");
            StringBuilder summaryText = new StringBuilder();
            List<DocumentationItemBuilder> typeParamElements = new List<DocumentationItemBuilder>();
            List<DocumentationItemBuilder> paramElements = new List<DocumentationItemBuilder>();
            StringBuilder returnsText = new StringBuilder();
            StringBuilder remarksText = new StringBuilder();
            StringBuilder exampleText = new StringBuilder();
            StringBuilder valueText = new StringBuilder();
            List<DocumentationItemBuilder> exception = new List<DocumentationItemBuilder>();

            using (var xml = XmlReader.Create(reader))
            {
                try
                {
                    xml.Read();
                    string elementName = null;
                    StringBuilder currentSectionBuilder = null;
                    do
                    {
                        if (xml.NodeType == XmlNodeType.Element)
                        {
                            elementName = xml.Name.ToLowerInvariant();
                            switch (elementName)
                            {
                                case "filterpriority":
                                    xml.Skip();
                                    break;
                                case "remarks":
                                    currentSectionBuilder = remarksText;
                                    break;
                                case "example":
                                    currentSectionBuilder = exampleText;
                                    break;
                                case "exception":
                                    DocumentationItemBuilder exceptionInstance = new DocumentationItemBuilder();
                                    exceptionInstance.Name = GetCref(xml["cref"]).TrimEnd();
                                    currentSectionBuilder = exceptionInstance.Documentation;
                                    exception.Add(exceptionInstance);
                                    break;
                                case "returns":
                                    currentSectionBuilder = returnsText;
                                    break;
                                case "summary":
                                    currentSectionBuilder = summaryText;
                                    break;
                                case "see":
                                    currentSectionBuilder.Append(GetCref(xml["cref"]));
                                    currentSectionBuilder.Append(xml["langword"]);
                                    break;
                                case "seealso":
                                    currentSectionBuilder.Append("See also: ");
                                    currentSectionBuilder.Append(GetCref(xml["cref"]));
                                    break;
                                case "paramref":
                                    currentSectionBuilder.Append(xml["name"]);
                                    currentSectionBuilder.Append(" ");
                                    break;
                                case "param":
                                    DocumentationItemBuilder paramInstance = new DocumentationItemBuilder();
                                    paramInstance.Name = TrimMultiLineString(xml["name"], lineEnding);
                                    currentSectionBuilder = paramInstance.Documentation;
                                    paramElements.Add(paramInstance);
                                    break;
                                case "typeparamref":
                                    currentSectionBuilder.Append(xml["name"]);
                                    currentSectionBuilder.Append(" ");
                                    break;
                                case "typeparam":
                                    DocumentationItemBuilder typeParamInstance = new DocumentationItemBuilder();
                                    typeParamInstance.Name = TrimMultiLineString(xml["name"], lineEnding);
                                    currentSectionBuilder = typeParamInstance.Documentation;
                                    typeParamElements.Add(typeParamInstance);
                                    break;
                                case "value":
                                    currentSectionBuilder = valueText;
                                    break;
                                case "br":
                                case "para":
                                    currentSectionBuilder.Append(lineEnding);
                                    break;
                            }
                        }
                        else if (xml.NodeType == XmlNodeType.Text && currentSectionBuilder != null)
                        {
                            if (elementName == "code")
                            {
                                currentSectionBuilder.Append(xml.Value);
                            }
                            else
                            {
                                currentSectionBuilder.Append(TrimMultiLineString(xml.Value, lineEnding));
                            }
                        }
                    } while (xml.Read());
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return new DocumentationComment(
                summaryText.ToString(),
                typeParamElements.Select(s => s.ConvertToDocumentedObject()).ToArray(),
                paramElements.Select(s => s.ConvertToDocumentedObject()).ToArray(),
                returnsText.ToString(),
                remarksText.ToString(),
                exampleText.ToString(),
                valueText.ToString(),
                exception.Select(s => s.ConvertToDocumentedObject()).ToArray());
        }

        private static string TrimMultiLineString(string input, string lineEnding)
        {
            var lines = input.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(lineEnding, lines.Select(l => l.TrimStart()));
        }

        private static string GetCref(string cref)
        {
            if (cref == null || cref.Trim().Length == 0)
            {
                return "";
            }
            if (cref.Length < 2)
            {
                return cref;
            }
            if (cref.Substring(1, 1) == ":")
            {
                return cref.Substring(2, cref.Length - 2) + " ";
            }
            return cref + " ";
        }

        //Gets the parameter documentation from this object
        public string GetParameterText(string name)
        {
            var requiredParam = Array.Find(ParamElements, parameter => parameter.Name == name);
            if (requiredParam != null)
                return requiredParam.Documentation;
            return string.Empty;
        }

        public static readonly DocumentationComment Empty = new DocumentationComment();
    }

    class DocumentationItemBuilder
    {
        public string Name { get; set; }
        public StringBuilder Documentation { get; set; }

        public DocumentationItemBuilder()
        {
            Documentation = new StringBuilder();
        }

        public DocumentationItem ConvertToDocumentedObject()
        {
            return new DocumentationItem(Name, Documentation.ToString());
        }
    }
}

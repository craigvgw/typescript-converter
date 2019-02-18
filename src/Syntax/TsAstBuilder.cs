﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace GrapeCity.CodeAnalysis.TypeScript.Syntax
{
    public class TsAstBuilder
    {
        public TsAstBuilder()
        {
        }

        public TsDocument Build(string path)
        {
            JObject jsonObject = JObject.Parse(File.ReadAllText(path));
            Node rootNode = this.Build(jsonObject);

            TsDocument document = new TsDocument()
            {
                Path = path,
                RootNode = rootNode,
            };

            return document;
        }

        public Node Build(JToken jsonToken)
        {
            Node node = this.Build(jsonToken, null);
            node.Normalize();

            return node;
        }

        private Node Build(JToken jsonToken, Node parent)
        {
            if (!jsonToken.HasValues)
            {
                return parent;
            }

            JTokenType tokenType = jsonToken.Type;

            if (tokenType == JTokenType.Object)
            {
                Node node = this.CreateNode(jsonToken as JObject);
                if (node != null)
                {
                    if (parent == null)
                    {
                        parent = node;
                    }
                    else
                    {
                        parent.AddNode(node);
                    }

                    foreach (var item in jsonToken)
                    {
                        this.Build(item, node);
                    }
                }
            }
            else
            {
                foreach (var item in jsonToken)
                {
                    this.Build(item, parent);
                }
            }

            return parent;
        }

        private Node CreateNode(JObject obj)
        {
            string syntaxKind = GetSyntaxNodeKey(obj);
            var nodeTypes = AllNodeTypes;

            if (nodeTypes.ContainsKey(syntaxKind))
            {
                Type type = nodeTypes[syntaxKind];
                ConstructorInfo constructorInfo = type.GetConstructor(Type.EmptyTypes);

                if (constructorInfo != null)
                {
                    Node syntaxNode = constructorInfo.Invoke(Type.EmptyTypes) as Node;
                    syntaxNode.Init(obj);
                    return syntaxNode;
                }
            }

            return null;
        }

        internal static string GetSyntaxNodeKey(JObject node)
        {
            string kind = node["kind"].ToString();
            if (Enum.TryParse(typeof(NodeKind), kind, out object result))
            {
                kind = result.ToString();
            }
            return kind;
        }

        #region Static Methods
        private static Dictionary<string, Type> _allNodeTypes;
        public static Dictionary<string, Type> AllNodeTypes
        {
            get
            {
                if (_allNodeTypes != null)
                {
                    return _allNodeTypes;
                }

                _allNodeTypes = new Dictionary<string, Type>();
                Type baseType = typeof(Node);
                Type[] types = Assembly.GetExecutingAssembly().GetExportedTypes();

                foreach (Type type in types)
                {
                    if (type.IsSubclassOf(baseType))
                    {
                        PropertyInfo p = type.GetProperty("Kind", typeof(NodeKind));
                        string kind = p.GetValue(type.GetConstructor(Type.EmptyTypes).Invoke(Type.EmptyTypes)).ToString();
                        _allNodeTypes[kind] = type;
                    }
                }

                return _allNodeTypes;
            }
        }
        #endregion
    }

}

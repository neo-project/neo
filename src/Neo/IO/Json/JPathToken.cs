// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Json
{
    sealed class JPathToken
    {
        public JPathTokenType Type { get; private set; }
        public string Content { get; private set; }

        public static IEnumerable<JPathToken> Parse(string expr)
        {
            for (int i = 0; i < expr.Length; i++)
            {
                JPathToken token = new();
                switch (expr[i])
                {
                    case '$':
                        token.Type = JPathTokenType.Root;
                        break;
                    case '.':
                        token.Type = JPathTokenType.Dot;
                        break;
                    case '[':
                        token.Type = JPathTokenType.LeftBracket;
                        break;
                    case ']':
                        token.Type = JPathTokenType.RightBracket;
                        break;
                    case '*':
                        token.Type = JPathTokenType.Asterisk;
                        break;
                    case ',':
                        token.Type = JPathTokenType.Comma;
                        break;
                    case ':':
                        token.Type = JPathTokenType.Colon;
                        break;
                    case '\'':
                        token.Type = JPathTokenType.String;
                        token.Content = ParseString(expr, i);
                        i += token.Content.Length - 1;
                        break;
                    case '_':
                    case >= 'a' and <= 'z':
                    case >= 'A' and <= 'Z':
                        token.Type = JPathTokenType.Identifier;
                        token.Content = ParseIdentifier(expr, i);
                        i += token.Content.Length - 1;
                        break;
                    case '-':
                    case >= '0' and <= '9':
                        token.Type = JPathTokenType.Number;
                        token.Content = ParseNumber(expr, i);
                        i += token.Content.Length - 1;
                        break;
                    default:
                        throw new FormatException();
                }
                yield return token;
            }
        }

        private static string ParseString(string expr, int start)
        {
            int end = start + 1;
            while (end < expr.Length)
            {
                char c = expr[end];
                end++;
                if (c == '\'') return expr[start..end];
            }
            throw new FormatException();
        }

        public static string ParseIdentifier(string expr, int start)
        {
            int end = start + 1;
            while (end < expr.Length)
            {
                char c = expr[end];
                if (c == '_' || c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9')
                    end++;
                else
                    break;
            }
            return expr[start..end];
        }

        private static string ParseNumber(string expr, int start)
        {
            int end = start + 1;
            while (end < expr.Length)
            {
                char c = expr[end];
                if (c >= '0' && c <= '9')
                    end++;
                else
                    break;
            }
            return expr[start..end];
        }

        private static JPathToken DequeueToken(Queue<JPathToken> tokens)
        {
            if (!tokens.TryDequeue(out JPathToken token))
                throw new FormatException();
            return token;
        }

        public static void ProcessJsonPath(ref JObject[] objects, Queue<JPathToken> tokens)
        {
            int maxDepth = 6;
            int maxObjects = 1024;
            while (tokens.Count > 0)
            {
                JPathToken token = DequeueToken(tokens);
                switch (token.Type)
                {
                    case JPathTokenType.Dot:
                        ProcessDot(ref objects, ref maxDepth, maxObjects, tokens);
                        break;
                    case JPathTokenType.LeftBracket:
                        ProcessBracket(ref objects, ref maxDepth, maxObjects, tokens);
                        break;
                    default:
                        throw new FormatException();
                }
            }
        }

        private static void ProcessDot(ref JObject[] objects, ref int maxDepth, int maxObjects, Queue<JPathToken> tokens)
        {
            JPathToken token = DequeueToken(tokens);
            switch (token.Type)
            {
                case JPathTokenType.Asterisk:
                    Descent(ref objects, ref maxDepth, maxObjects);
                    break;
                case JPathTokenType.Dot:
                    ProcessRecursiveDescent(ref objects, ref maxDepth, maxObjects, tokens);
                    break;
                case JPathTokenType.Identifier:
                    Descent(ref objects, ref maxDepth, maxObjects, token.Content);
                    break;
                default:
                    throw new FormatException();
            }
        }

        private static void ProcessBracket(ref JObject[] objects, ref int maxDepth, int maxObjects, Queue<JPathToken> tokens)
        {
            JPathToken token = DequeueToken(tokens);
            switch (token.Type)
            {
                case JPathTokenType.Asterisk:
                    if (DequeueToken(tokens).Type != JPathTokenType.RightBracket)
                        throw new FormatException();
                    Descent(ref objects, ref maxDepth, maxObjects);
                    break;
                case JPathTokenType.Colon:
                    ProcessSlice(ref objects, ref maxDepth, maxObjects, tokens, 0);
                    break;
                case JPathTokenType.Number:
                    JPathToken next = DequeueToken(tokens);
                    switch (next.Type)
                    {
                        case JPathTokenType.Colon:
                            ProcessSlice(ref objects, ref maxDepth, maxObjects, tokens, int.Parse(token.Content));
                            break;
                        case JPathTokenType.Comma:
                            ProcessUnion(ref objects, ref maxDepth, maxObjects, tokens, token);
                            break;
                        case JPathTokenType.RightBracket:
                            Descent(ref objects, ref maxDepth, maxObjects, int.Parse(token.Content));
                            break;
                        default:
                            throw new FormatException();
                    }
                    break;
                case JPathTokenType.String:
                    next = DequeueToken(tokens);
                    switch (next.Type)
                    {
                        case JPathTokenType.Comma:
                            ProcessUnion(ref objects, ref maxDepth, maxObjects, tokens, token);
                            break;
                        case JPathTokenType.RightBracket:
                            Descent(ref objects, ref maxDepth, maxObjects, JObject.Parse($"\"{token.Content.Trim('\'')}\"").GetString());
                            break;
                        default:
                            throw new FormatException();
                    }
                    break;
                default:
                    throw new FormatException();
            }
        }

        private static void ProcessRecursiveDescent(ref JObject[] objects, ref int maxDepth, int maxObjects, Queue<JPathToken> tokens)
        {
            List<JObject> results = new();
            JPathToken token = DequeueToken(tokens);
            if (token.Type != JPathTokenType.Identifier) throw new FormatException();
            while (objects.Length > 0)
            {
                results.AddRange(objects.Where(p => p is not null).SelectMany(p => p.Properties).Where(p => p.Key == token.Content).Select(p => p.Value));
                Descent(ref objects, ref maxDepth, maxObjects);
                if (results.Count > maxObjects) throw new InvalidOperationException(nameof(maxObjects));
            }
            objects = results.ToArray();
        }

        private static void ProcessSlice(ref JObject[] objects, ref int maxDepth, int maxObjects, Queue<JPathToken> tokens, int start)
        {
            JPathToken token = DequeueToken(tokens);
            switch (token.Type)
            {
                case JPathTokenType.Number:
                    if (DequeueToken(tokens).Type != JPathTokenType.RightBracket)
                        throw new FormatException();
                    DescentRange(ref objects, ref maxDepth, maxObjects, start, int.Parse(token.Content));
                    break;
                case JPathTokenType.RightBracket:
                    DescentRange(ref objects, ref maxDepth, maxObjects, start, 0);
                    break;
                default:
                    throw new FormatException();
            }
        }

        private static void ProcessUnion(ref JObject[] objects, ref int maxDepth, int maxObjects, Queue<JPathToken> tokens, JPathToken first)
        {
            List<JPathToken> items = new() { first };
            while (true)
            {
                JPathToken token = DequeueToken(tokens);
                if (token.Type != first.Type) throw new FormatException();
                items.Add(token);
                token = DequeueToken(tokens);
                if (token.Type == JPathTokenType.RightBracket)
                    break;
                if (token.Type != JPathTokenType.Comma)
                    throw new FormatException();
            }
            switch (first.Type)
            {
                case JPathTokenType.Number:
                    Descent(ref objects, ref maxDepth, maxObjects, items.Select(p => int.Parse(p.Content)).ToArray());
                    break;
                case JPathTokenType.String:
                    Descent(ref objects, ref maxDepth, maxObjects, items.Select(p => JObject.Parse($"\"{p.Content.Trim('\'')}\"").GetString()).ToArray());
                    break;
                default:
                    throw new FormatException();
            }
        }

        private static void Descent(ref JObject[] objects, ref int maxDepth, int maxObjects)
        {
            if (maxDepth <= 0) throw new InvalidOperationException();
            --maxDepth;
            objects = objects.Where(p => p is not null).SelectMany(p => p is JArray array ? array : p.Properties.Values).ToArray();
            if (objects.Length > maxObjects) throw new InvalidOperationException(nameof(maxObjects));
        }

        private static void Descent(ref JObject[] objects, ref int maxDepth, int maxObjects, params string[] names)
        {
            static IEnumerable<JObject> GetProperties(JObject obj, string[] names)
            {
                foreach (string name in names)
                    if (obj.ContainsProperty(name))
                        yield return obj[name];
            }
            if (maxDepth <= 0) throw new InvalidOperationException();
            --maxDepth;
            objects = objects.SelectMany(p => GetProperties(p, names)).ToArray();
            if (objects.Length > maxObjects) throw new InvalidOperationException(nameof(maxObjects));
        }

        private static void Descent(ref JObject[] objects, ref int maxDepth, int maxObjects, params int[] indexes)
        {
            static IEnumerable<JObject> GetElements(JArray array, int[] indexes)
            {
                foreach (int index in indexes)
                {
                    int i = index >= 0 ? index : index + array.Count;
                    if (i >= 0 && i < array.Count)
                        yield return array[i];
                }
            }
            if (maxDepth <= 0) throw new InvalidOperationException();
            --maxDepth;
            objects = objects.OfType<JArray>().SelectMany(p => GetElements(p, indexes)).ToArray();
            if (objects.Length > maxObjects) throw new InvalidOperationException(nameof(maxObjects));
        }

        private static void DescentRange(ref JObject[] objects, ref int maxDepth, int maxObjects, int start, int end)
        {
            if (maxDepth <= 0) throw new InvalidOperationException();
            --maxDepth;
            objects = objects.OfType<JArray>().SelectMany(p =>
            {
                int iStart = start >= 0 ? start : start + p.Count;
                if (iStart < 0) iStart = 0;
                int iEnd = end > 0 ? end : end + p.Count;
                int count = iEnd - iStart;
                return p.Skip(iStart).Take(count);
            }).ToArray();
            if (objects.Length > maxObjects) throw new InvalidOperationException(nameof(maxObjects));
        }
    }
}

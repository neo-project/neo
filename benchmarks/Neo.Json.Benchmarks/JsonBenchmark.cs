// Copyright (C) 2015-2024 The Neo Project.
//
// JsonBenchmark.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;

namespace Neo.Json.Benchmarks
{
    [MemoryDiagnoser]  // 开启内存诊断器
    [CsvMeasurementsExporter]  // 导出 CSV 格式的结果
    [MarkdownExporter]  // 导出 Markdown 格式的结果
    public class JsonBenchmark
    {
        private string jsonString;
        private List<RpcTestCase> testCases;

        [GlobalSetup]
        public void Setup()
        {
            // 读取 JSON 文件
            jsonString = File.ReadAllText("Data/RpcTestCases.json");
        }

        /// <summary>
        /// 使用 Newtonsoft.Json 进行反序列化
        /// </summary>
        [Benchmark]
        public List<RpcTestCaseN> Newtonsoft_Deserialize()
        {
            return JsonConvert.DeserializeObject<List<RpcTestCaseN>>(jsonString);
        }

        /// <summary>
        /// 使用 Neo.Json 进行反序列化（支持嵌套解析）
        /// </summary>
        [Benchmark]
        public List<RpcTestCase> NeoJson_Deserialize()
        {
            var result = new List<RpcTestCase>();

            // 解析为 JArray
            var neoJsonObject = Neo.Json.JArray.Parse(jsonString);

            foreach (var item in neoJsonObject as JArray)
            {
                var testCase = new RpcTestCase
                {
                    Name = item["Name"].GetString(),
                    Request = new RpcRequest
                    {
                        JsonRpc = item["Request"]["jsonrpc"].GetString(),
                        Method = item["Request"]["method"].GetString(),
                        Params = ConvertToJTokenArray(item["Request"]["params"]),
                        Id = (int)item["Request"]["id"].GetNumber()
                    },
                    Response = new RpcResponse
                    {
                        JsonRpc = item["Response"]["jsonrpc"].GetString(),
                        Id = (int)item["Response"]["id"].GetNumber(),
                        Result = item["Response"]["result"]
                    }
                };
                result.Add(testCase);
            }
            return result;
        }

        /// <summary>
        /// 递归解析 params 和 stack 数组
        /// </summary>
        private List<object> ParseParams(Neo.Json.JToken token)
        {
            var result = new List<object>();

            if (token is Neo.Json.JArray array)
            {
                // ✅ 使用 foreach 正确解析 JArray
                foreach (var item in array)
                {
                    result.Add(ParseParams(item));
                }
            }
            else if (token is Neo.Json.JObject obj)
            {
                // ✅ 使用 Neo.Json.JObject 的 Properties 遍历
                var dict = new Dictionary<string, object>();
                foreach (var property in obj.Properties)
                {
                    dict[property.Key] = property.Value.GetString();
                }
                result.Add(dict);
            }
            else
            {
                // ✅ 如果是普通值，直接添加
                result.Add(token.GetString());
            }

            return result;
        }

        /// <summary>
        /// 将任意类型的 JSON 解析为 JToken[]（适用于嵌套结构）
        /// </summary>
        private Neo.Json.JToken[] ConvertToJTokenArray(Neo.Json.JToken token)
        {
            var result = new List<Neo.Json.JToken>();

            if (token is Neo.Json.JArray array)
            {
                // ✅ 如果是 JArray，则逐个解析并添加到结果中
                foreach (var item in array)
                {
                    result.AddRange(ConvertToJTokenArray(item));
                }
            }
            else if (token is Neo.Json.JObject obj)
            {
                // ✅ 将 JObject 转换为 JToken（Dictionary 风格）
                var newObj = new Neo.Json.JObject();
                foreach (var property in obj.Properties)
                {
                    newObj[property.Key] = new Neo.Json.JString(property.Value.GetString());
                }
                result.Add(newObj);
            }
            else
            {
                // ✅ 直接添加基础类型 JToken
                result.Add(token);
            }

            return result.ToArray();  // ✅ 将 List 转换为 JToken 数组
        }
    }
}

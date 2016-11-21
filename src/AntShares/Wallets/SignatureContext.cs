using AntShares.Compiler;
using AntShares.Core;
using AntShares.Cryptography.ECC;
using AntShares.IO.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace AntShares.Wallets
{
    /// <summary>
    /// 签名上下文
    /// </summary>
    public class SignatureContext
    {
        /// <summary>
        /// 要签名的数据
        /// </summary>
        public readonly ISignable Signable;
        /// <summary>
        /// 要验证的脚本散列值
        /// </summary>
        public readonly UInt160[] ScriptHashes;
        private readonly byte[][] redeemScripts;
        private readonly byte[][][] parameters;
        private readonly JObject[] temp;

        /// <summary>
        /// 判断签名是否完成
        /// </summary>
        public bool Completed
        {
            get
            {
                return parameters.All(p => p != null && p.All(q => q != null));
            }
        }

        /// <summary>
        /// 对指定的数据构造签名上下文
        /// </summary>
        /// <param name="signable">要签名的数据</param>
        public SignatureContext(ISignable signable)
        {
            this.Signable = signable;
            this.ScriptHashes = signable.GetScriptHashesForVerifying();
            this.redeemScripts = new byte[ScriptHashes.Length][];
            this.parameters = new byte[ScriptHashes.Length][][];
            this.temp = new JObject[ScriptHashes.Length];
        }

        public bool Add(Contract contract, int index, byte[] parameter)
        {
            int i = GetIndex(contract.ScriptHash);
            if (i < 0) return false;
            if (redeemScripts[i] == null)
                redeemScripts[i] = contract.RedeemScript;
            if (parameters[i] == null)
                parameters[i] = new byte[contract.ParameterList.Length][];
            parameters[i][index] = parameter;
            return true;
        }

        public bool AddSignature(Contract contract, ECPoint pubkey, byte[] signature)
        {
            if (contract.Type == ContractType.MultiSigContract)
            {
                int index = GetIndex(contract.ScriptHash);
                if (index < 0) return false;
                if (redeemScripts[index] == null)
                    redeemScripts[index] = contract.RedeemScript;
                if (parameters[index] == null)
                    parameters[index] = new byte[contract.ParameterList.Length][];
                if (temp[index] == null) temp[index] = new JArray();
                JArray array = (JArray)temp[index];
                JObject obj = new JObject();
                obj["pubkey"] = pubkey.EncodePoint(true).ToHexString();
                obj["signature"] = signature.ToHexString();
                array.Add(obj);
                if (array.Count == contract.ParameterList.Length)
                {
                    List<ECPoint> points = new List<ECPoint>();
                    {
                        int i = 0;
                        switch (contract.RedeemScript[i++])
                        {
                            case 1:
                                ++i;
                                break;
                            case 2:
                                i += 2;
                                break;
                        }
                        while (contract.RedeemScript[i++] == 33)
                        {
                            points.Add(ECPoint.DecodePoint(contract.RedeemScript.Skip(i).Take(33).ToArray(), ECCurve.Secp256r1));
                            i += 33;
                        }
                    }
                    Dictionary<ECPoint, int> dic = points.Select((p, i) => new
                    {
                        PublicKey = p,
                        Index = i
                    }).ToDictionary(p => p.PublicKey, p => p.Index);
                    byte[][] sigs = array.Select(p => new
                    {
                        Signature = p["signature"].AsString().HexToBytes(),
                        Index = dic[ECPoint.DecodePoint(p["pubkey"].AsString().HexToBytes(), ECCurve.Secp256r1)]
                    }).OrderBy(p => p.Index).Select(p => p.Signature).ToArray();
                    for (int i = 0; i < sigs.Length; i++)
                        if (!Add(contract, i, sigs[i]))
                            throw new InvalidOperationException();
                    temp[index] = null;
                }
                return true;
            }
            else
            {
                int index = -1;
                for (int i = 0; i < contract.ParameterList.Length; i++)
                    if (contract.ParameterList[i] == ContractParameterType.Signature)
                        if (index >= 0)
                            throw new NotSupportedException();
                        else
                            index = i;
                return Add(contract, index, signature);
            }
        }

        /// <summary>
        /// 从指定的json对象中解析出签名上下文
        /// </summary>
        /// <param name="json">json对象</param>
        /// <returns>返回上下文</returns>
        public static SignatureContext FromJson(JObject json)
        {
            ISignable signable = typeof(SignatureContext).GetTypeInfo().Assembly.CreateInstance(json["type"].AsString()) as ISignable;
            using (MemoryStream ms = new MemoryStream(json["hex"].AsString().HexToBytes(), false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                signable.DeserializeUnsigned(reader);
            }
            SignatureContext context = new SignatureContext(signable);
            JArray scripts = (JArray)json["scripts"];
            for (int i = 0; i < scripts.Count; i++)
            {
                if (scripts[i] != null)
                {
                    context.redeemScripts[i] = scripts[i]["redeem_script"].AsString().HexToBytes();
                    JArray ps = (JArray)scripts[i]["parameters"];
                    context.parameters[i] = new byte[ps.Count][];
                    for (int j = 0; j < ps.Count; j++)
                    {
                        context.parameters[i][j] = ps[j]?.AsString().HexToBytes();
                    }
                    context.temp[i] = scripts[i]["temp"];
                }
            }
            return context;
        }

        private int GetIndex(UInt160 scriptHash)
        {
            for (int i = 0; i < ScriptHashes.Length; i++)
                if (ScriptHashes[i].Equals(scriptHash))
                    return i;
            return -1;
        }

        public byte[] GetParameter(UInt160 scriptHash, int index)
        {
            return parameters[GetIndex(scriptHash)][index];
        }

        /// <summary>
        /// 从签名上下文中获得完整签名的合约脚本
        /// </summary>
        /// <returns>返回合约脚本</returns>
        public Witness[] GetScripts()
        {
            if (!Completed) throw new InvalidOperationException();
            Witness[] scripts = new Witness[parameters.Length];
            for (int i = 0; i < scripts.Length; i++)
            {
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    foreach (byte[] parameter in parameters[i])
                    {
                        if (parameter.Length <= 2)
                            sb.Push(new BigInteger(parameter));
                        else
                            sb.Push(parameter);
                    }
                    scripts[i] = new Witness
                    {
                        StackScript = sb.ToArray(),
                        RedeemScript = redeemScripts[i]
                    };
                }
            }
            return scripts;
        }

        public static SignatureContext Parse(string value)
        {
            return FromJson(JObject.Parse(value));
        }

        /// <summary>
        /// 把签名上下文转为json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public JObject ToJson()
        {
            JObject json = new JObject();
            json["type"] = Signable.GetType().FullName;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                Signable.SerializeUnsigned(writer);
                writer.Flush();
                json["hex"] = ms.ToArray().ToHexString();
            }
            JArray scripts = new JArray();
            for (int i = 0; i < redeemScripts.Length; i++)
            {
                if (redeemScripts[i] == null)
                {
                    scripts.Add(null);
                }
                else
                {
                    scripts.Add(new JObject());
                    scripts[i]["redeem_script"] = redeemScripts[i].ToHexString();
                    JArray ps = new JArray();
                    foreach (byte[] parameter in parameters[i])
                    {
                        ps.Add(parameter?.ToHexString());
                    }
                    scripts[i]["parameters"] = ps;
                    scripts[i]["temp"] = temp[i];
                }
            }
            json["scripts"] = scripts;
            return json;
        }

        public override string ToString()
        {
            return ToJson().ToString();
        }
    }
}

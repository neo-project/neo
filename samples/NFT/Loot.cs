// Copyright (C) 2021 Neo Core.
// This file belongs to the NEO-GAME-Loot contract developed for neo N3
//
// The NEO-GAME-Loot is free smart contract distributed under the MIT software 
// license, see the accompanying file LICENSE in the main directory of
// the project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Attributes;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace NFT
{
    [ManifestExtra("Author", "core-dev")]
    [ManifestExtra("Email", "core@neo.org")]
    [DisplayName("Secure-Loot")]
    [ManifestExtra("Description", "This is a text NFT game developed for neo N3.")]
    [SupportedStandards("NEP-11")]
    [ContractPermission("*", "onNEP11Payment")]
    public partial class Loot : Nep11Token<TokenState>
    {
        public string SourceCode => "https://github.com/neo-project/samples/Loot";
        public override string Symbol() => "sLoot";
        private static readonly StorageMap TokenIndexMap = new(Storage.CurrentContext, (byte)StoragePrefix.Token);
        private static readonly StorageMap TokenMap = new(Storage.CurrentContext, Prefix_Token);
        public static event Action<string> EventMsg;

        [Safe]
        public override Map<string, object> Properties(ByteString tokenId)
        {
            Tools.Require(Runtime.EntryScriptHash == Runtime.CallingScriptHash);
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            TokenState token = (TokenState)StdLib.Deserialize(tokenMap[tokenId]);
            Map<string, object> map = new()
            {
                ["name"] = token.Name,
                ["owner"] = token.Owner,
                ["tokenID"] = token.TokenId,
                ["credential"] = token.Credential
            };
            return map;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private TokenState GetToken(BigInteger tokenId)
        {
            TokenState token = (TokenState)StdLib.Deserialize(TokenMap[tokenId.ToString()]);
            Tools.Require(token is not null, "Token not exists");
            return token;
        }

        [Safe]
        public BigInteger GetCredential(BigInteger tokenId) => GetToken(tokenId).Credential;

        [Safe]
        public string GetWeapon(BigInteger credential) => Pluck(credential, 0xd7a595, _weapons);

        [Safe]
        public string GetChest(BigInteger credential) => Pluck(credential, 0x5a7e36, _chestArmor);

        [Safe]
        public string GetHead(BigInteger credential) => Pluck(credential, 0x0cdfbb, _headArmor);

        [Safe]
        public string GetWaist(BigInteger credential) => Pluck(credential, 0x7dcd1b, _waistArmor);

        [Safe]
        public string GetFoot(BigInteger credential) => Pluck(credential, 0x92877a, _footArmor);

        [Safe]
        public string GetHand(BigInteger credential) => Pluck(credential, 0x323282, _handArmor);

        [Safe]
        public string GetNeck(BigInteger credential) => Pluck(credential, 0x0d59c2, _necklaces);

        [Safe]
        public string GetRing(BigInteger credential) => Pluck(credential, 0xfae431, _rings);

        [Safe]
        private string Pluck(BigInteger credential, BigInteger keyPrefix, string[] sourceArray)
        {
            var rand = credential ^ keyPrefix;
            var value = rand % sourceArray.Length;
            string output = sourceArray[(int)rand % sourceArray.Length];
            var greatness = rand % 21;

            value = rand % _suffixes.Length;
            if (greatness > 14) output = $"{output} {_suffixes[(int)value]}";
            if (greatness < 19) return output;
            value = rand % _namePrefixes.Length;
            var name0 = _namePrefixes[(int)value];
            value = rand % _nameSuffixes.Length;
            var name1 = _nameSuffixes[(int)value];
            output = greatness == 19 ? $"\"{name0} {name1}\" {output}" : $"\"{name0} {name1}\" {output} +1";
            return output;
        }


        [Safe]
        public string tokenURI(BigInteger tokenId)
        {
            var token = GetToken(tokenId);
            var parts = new string[19];
            parts[0] = "<svg xmlns=\"http://www.w3.org/2000/svg\" preserveAspectRatio=\"xMinYMin meet\" viewBox=\"0 0 350 350\">" +
                "<style>.base { fill: white; font-family: serif; font-size: 14px; }</style>" +
                "<rect width=\"100%\" height=\"100%\" fill=\"black\" />" +
                "<text x=\"10\" y=\"20\" class=\"base\">";
            parts[1] = $"{token.Name}";
            parts[2] = "</text><text x=\"10\" y=\"40\" class=\"base\">";
            parts[3] = GetWeapon(token.Credential);
            parts[4] = "</text><text x=\"10\" y=\"60\" class=\"base\">";
            parts[5] = GetChest(token.Credential);
            parts[6] = "</text><text x=\"10\" y=\"80\" class=\"base\">";
            parts[7] = GetHead(token.Credential);
            parts[8] = "</text><text x=\"10\" y=\"100\" class=\"base\">";
            parts[9] = GetWaist(token.Credential);
            parts[10] = "</text><text x=\"10\" y=\"120\" class=\"base\">";
            parts[11] = GetFoot(token.Credential);
            parts[12] = "</text><text x=\"10\" y=\"140\" class=\"base\">";
            parts[13] = GetHand(token.Credential);
            parts[14] = "</text><text x=\"10\" y=\"160\" class=\"base\">";
            parts[15] = GetNeck(token.Credential);
            parts[16] = "</text><text x=\"10\" y=\"180\" class=\"base\">";
            parts[17] = GetRing(token.Credential);
            parts[18] = "</text></svg>";

            string output = $"{parts[0]} {parts[1]} {parts[2]} {parts[3]} {parts[4]} {parts[5]} {parts[6]} {parts[7]} {parts[8]}";
            output = $"{output} {parts[9]} {parts[10]} {parts[11]} {parts[12]} {parts[13]} {parts[14]} {parts[15]} {parts[16]} {parts[17]} {parts[18]}";
            //string json = StdLib.Base64Encode($"{{\"name\": \"Bag # {tokenId.ToString()}\", \"description\": \"Loot is randomized adventurer gear generated and stored on chain.Stats, images, and other functionality are intentionally omitted for others to interpret.Feel free to use Loot in any way you want.\", \"image\": \"data:image / svg + xml; base64, { StdLib.Base64Encode(output)} \"}}");
            //output = $"data:application/json;base64, {json}";
            return output;
        }

        /// <summary>
        /// Security Requirements:
        /// 
        /// <0> Has to check the validity of the token Id 
        ///     both the upper and lower bound
        /// 
        /// <1> shall not be called from a contract
        /// 
        /// <3> tx shall fault if token already taken
        /// 
        /// <4> update the token map.
        /// </summary>
        /// <param name="tokenId"></param>
        public void Claim(BigInteger tokenId)
        {
            // 222 reserved to the developer
            Tools.Require(!tokenId.IsZero && tokenId < 7778, "Token ID invalid");
            Tools.Require(Runtime.EntryScriptHash == Runtime.CallingScriptHash, "Contract calls are not allowed");
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            MintToken(tokenId, tx.Sender);
            EventMsg("Player mints success");
        }

        /// <summary>
        /// Security Requirements:
        /// 
        /// <0> only the owner can call this function
        /// 
        /// <1> the range of the tokenid is to be in (7777, 8001)
        /// </summary>
        /// <param name="tokenId"></param>
        public void OwnerClaim(BigInteger tokenId)
        {
            OwnerOnly();
            Tools.Require(tokenId > 7777 && tokenId < 8001, "Token ID invalid");
            var sender = GetOwner();
            MintToken(tokenId, sender);
            EventMsg("Owner mints success");
        }

        /// <summary>
        /// Security Requirements:
        /// 
        /// <0> the transaction should `FAULT` if the token already taken
        /// 
        /// <1> has to update the taken map if a new token is mint.
        /// 
        /// </summary>
        /// <param name="tokenId"></param>
        /// <param name="sender"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MintToken(BigInteger tokenId, UInt160 sender)
        {
            var credential = CheckClaim(tokenId);
            TokenState token = TokenState.MintLoot(sender, tokenId, credential);
            Mint(tokenId.ToString(), token);
            TokenIndexMap.Put(tokenId.ToString(), "taken");
        }

        /// <summary>
        /// Security requirements:
        /// 
        /// <0> throw exception if token already taken
        /// 
        /// <1> should get a random number as credential that
        ///     is not predictable and not linked to the tokenId
        /// </summary>
        /// <param name="tokenId"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private BigInteger CheckClaim(BigInteger tokenId)
        {
            // <0> -- confirmed
            Tools.Require(TokenIndexMap.Get(tokenId.ToString()) != "taken", "Token already claimed.");
            // <1> -- confirmed
            return Runtime.GetRandom();
        }
    }

    internal enum StoragePrefix
    {
        Owner = 0x15,
        Token = 0x16,
    }
}

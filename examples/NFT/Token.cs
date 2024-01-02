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

using System.Numerics;
using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services;
namespace NFT
{
    public class TokenState : Nep11TokenState
    {
        public BigInteger TokenId;

        public BigInteger Credential;

        public static TokenState MintLoot(UInt160 owner, BigInteger tokenId, BigInteger credential) => new(owner, tokenId, credential);

        private TokenState(UInt160 owner, BigInteger tokenId, BigInteger credential)
        {
            Owner = owner;
            TokenId = tokenId;
            Credential = credential;
            Name = "N3 Secure Loot #" + TokenId;
        }

        public void OwnerOnly()
        {
            Tools.Require(Runtime.CheckWitness(Owner), "Authorization failed.");
        }
    }
}

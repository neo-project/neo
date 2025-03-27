// Copyright (C) 2015-2025 The Neo Project.
//
// CharacterMutations.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System;
using System.Text;

namespace Neo.Json.Fuzzer.Generators
{
    /// <summary>
    /// Provides character-level mutation strategies for the mutation engine
    /// </summary>
    public class CharacterMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;
        
        /// <summary>
        /// Initializes a new instance of the CharacterMutations class
        /// </summary>
        public CharacterMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }
        
        /// <summary>
        /// Applies a character-level mutation to the JSON
        /// </summary>
        public string ApplyCharacterMutation(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return "{}";
            }
            
            int strategy = _random.Next(5);
            
            return strategy switch
            {
                0 => InsertRandomCharacter(json),
                1 => RemoveRandomCharacter(json),
                2 => ReplaceRandomCharacter(json),
                3 => DuplicateRandomSection(json),
                _ => RemoveRandomSection(json)
            };
        }
        
        /// <summary>
        /// Inserts a random character into the JSON string
        /// </summary>
        public string InsertRandomCharacter(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }
            
            int position = _random.Next(json.Length);
            char character = GetRandomJsonCharacter();
            
            return json.Insert(position, character.ToString());
        }

        /// <summary>
        /// Removes a random character from the JSON string
        /// </summary>
        public string RemoveRandomCharacter(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }
            
            int position = _random.Next(json.Length);
            
            return json.Remove(position, 1);
        }

        /// <summary>
        /// Replaces a random character in the JSON string
        /// </summary>
        public string ReplaceRandomCharacter(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }
            
            int position = _random.Next(json.Length);
            char character = GetRandomJsonCharacter();
            
            StringBuilder sb = new(json);
            sb[position] = character;
            
            return sb.ToString();
        }

        /// <summary>
        /// Duplicates a random section of the JSON string
        /// </summary>
        public string DuplicateRandomSection(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length < 2)
            {
                return json;
            }
            
            int length = Math.Min(10, json.Length / 2);
            int start = _random.Next(json.Length - length);
            int insertPosition = _random.Next(json.Length);
            
            string section = json.Substring(start, length);
            
            return json.Insert(insertPosition, section);
        }

        /// <summary>
        /// Removes a random section of the JSON string
        /// </summary>
        public string RemoveRandomSection(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length < 3)
            {
                return json;
            }
            
            int length = Math.Min(3, json.Length / 3);
            int start = _random.Next(json.Length - length);
            
            return json.Remove(start, length);
        }
        
        /// <summary>
        /// Gets a random character that might appear in JSON
        /// </summary>
        private char GetRandomJsonCharacter()
        {
            string jsonChars = "{}[]\":,0123456789.truefalsnl ";
            return jsonChars[_random.Next(jsonChars.Length)];
        }
    }
}

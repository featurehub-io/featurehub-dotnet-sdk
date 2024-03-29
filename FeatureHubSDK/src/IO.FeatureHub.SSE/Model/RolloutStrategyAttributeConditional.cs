/*
 * FeatureServiceApi
 *
 * This describes the API clients use for accessing features
 *
 * The version of the OpenAPI document: 1.1.4
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using OpenAPIDateConverter = IO.FeatureHub.SSE.Client.OpenAPIDateConverter;

namespace IO.FeatureHub.SSE.Model
{
    /// <summary>
    /// Defines RolloutStrategyAttributeConditional
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RolloutStrategyAttributeConditional
    {
        /// <summary>
        /// Enum EQUALS for value: EQUALS
        /// </summary>
        [EnumMember(Value = "EQUALS")]
        EQUALS = 1,

        /// <summary>
        /// Enum ENDSWITH for value: ENDS_WITH
        /// </summary>
        [EnumMember(Value = "ENDS_WITH")]
        ENDSWITH = 2,

        /// <summary>
        /// Enum STARTSWITH for value: STARTS_WITH
        /// </summary>
        [EnumMember(Value = "STARTS_WITH")]
        STARTSWITH = 3,

        /// <summary>
        /// Enum GREATER for value: GREATER
        /// </summary>
        [EnumMember(Value = "GREATER")]
        GREATER = 4,

        /// <summary>
        /// Enum GREATEREQUALS for value: GREATER_EQUALS
        /// </summary>
        [EnumMember(Value = "GREATER_EQUALS")]
        GREATEREQUALS = 5,

        /// <summary>
        /// Enum LESS for value: LESS
        /// </summary>
        [EnumMember(Value = "LESS")]
        LESS = 6,

        /// <summary>
        /// Enum LESSEQUALS for value: LESS_EQUALS
        /// </summary>
        [EnumMember(Value = "LESS_EQUALS")]
        LESSEQUALS = 7,

        /// <summary>
        /// Enum NOTEQUALS for value: NOT_EQUALS
        /// </summary>
        [EnumMember(Value = "NOT_EQUALS")]
        NOTEQUALS = 8,

        /// <summary>
        /// Enum INCLUDES for value: INCLUDES
        /// </summary>
        [EnumMember(Value = "INCLUDES")]
        INCLUDES = 9,

        /// <summary>
        /// Enum EXCLUDES for value: EXCLUDES
        /// </summary>
        [EnumMember(Value = "EXCLUDES")]
        EXCLUDES = 10,

        /// <summary>
        /// Enum REGEX for value: REGEX
        /// </summary>
        [EnumMember(Value = "REGEX")]
        REGEX = 11

    }

}

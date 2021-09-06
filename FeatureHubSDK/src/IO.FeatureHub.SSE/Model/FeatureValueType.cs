/*
 * FeatureServiceApi
 *
 * This describes the API clients use for accessing features
 *
 * The version of the OpenAPI document: 1.1.2
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
    /// Defines FeatureValueType
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FeatureValueType
    {
        /// <summary>
        /// Enum BOOLEAN for value: BOOLEAN
        /// </summary>
        [EnumMember(Value = "BOOLEAN")]
        BOOLEAN = 1,

        /// <summary>
        /// Enum STRING for value: STRING
        /// </summary>
        [EnumMember(Value = "STRING")]
        STRING = 2,

        /// <summary>
        /// Enum NUMBER for value: NUMBER
        /// </summary>
        [EnumMember(Value = "NUMBER")]
        NUMBER = 3,

        /// <summary>
        /// Enum JSON for value: JSON
        /// </summary>
        [EnumMember(Value = "JSON")]
        JSON = 4

    }

}

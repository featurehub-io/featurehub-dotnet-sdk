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
    /// RolloutStrategyAttribute
    /// </summary>
    [DataContract(Name = "RolloutStrategyAttribute")]
    public partial class RolloutStrategyAttribute : IEquatable<RolloutStrategyAttribute>
    {

        /// <summary>
        /// Gets or Sets Conditional
        /// </summary>
        [DataMember(Name = "conditional", EmitDefaultValue = false)]
        public RolloutStrategyAttributeConditional? Conditional { get; set; }

        /// <summary>
        /// Gets or Sets Type
        /// </summary>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public RolloutStrategyFieldType? Type { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="RolloutStrategyAttribute" /> class.
        /// </summary>
        /// <param name="id">A temporary id used only when validating. Saving strips these out as they are not otherwise necessary.</param>
        /// <param name="conditional">conditional.</param>
        /// <param name="fieldName">fieldName.</param>
        /// <param name="values">the value(s) associated with this rule.</param>
        /// <param name="type">type.</param>
        public RolloutStrategyAttribute(string id = default(string), RolloutStrategyAttributeConditional? conditional = default(RolloutStrategyAttributeConditional?), string fieldName = default(string), List<object> values = default(List<object>), RolloutStrategyFieldType? type = default(RolloutStrategyFieldType?))
        {
            this.Id = id;
            this.Conditional = conditional;
            this.FieldName = fieldName;
            this.Values = values;
            this.Type = type;
        }

        /// <summary>
        /// A temporary id used only when validating. Saving strips these out as they are not otherwise necessary
        /// </summary>
        /// <value>A temporary id used only when validating. Saving strips these out as they are not otherwise necessary</value>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or Sets FieldName
        /// </summary>
        [DataMember(Name = "fieldName", EmitDefaultValue = false)]
        public string FieldName { get; set; }

        /// <summary>
        /// the value(s) associated with this rule
        /// </summary>
        /// <value>the value(s) associated with this rule</value>
        [DataMember(Name = "values", EmitDefaultValue = false)]
        public List<object> Values { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class RolloutStrategyAttribute {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Conditional: ").Append(Conditional).Append("\n");
            sb.Append("  FieldName: ").Append(FieldName).Append("\n");
            sb.Append("  Values: ").Append(Values).Append("\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as RolloutStrategyAttribute);
        }

        /// <summary>
        /// Returns true if RolloutStrategyAttribute instances are equal
        /// </summary>
        /// <param name="input">Instance of RolloutStrategyAttribute to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(RolloutStrategyAttribute input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Id == input.Id ||
                    (this.Id != null &&
                    this.Id.Equals(input.Id))
                ) && 
                (
                    this.Conditional == input.Conditional ||
                    this.Conditional.Equals(input.Conditional)
                ) && 
                (
                    this.FieldName == input.FieldName ||
                    (this.FieldName != null &&
                    this.FieldName.Equals(input.FieldName))
                ) && 
                (
                    this.Values == input.Values ||
                    this.Values != null &&
                    input.Values != null &&
                    this.Values.SequenceEqual(input.Values)
                ) && 
                (
                    this.Type == input.Type ||
                    this.Type.Equals(input.Type)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Id != null)
                    hashCode = hashCode * 59 + this.Id.GetHashCode();
                hashCode = hashCode * 59 + this.Conditional.GetHashCode();
                if (this.FieldName != null)
                    hashCode = hashCode * 59 + this.FieldName.GetHashCode();
                if (this.Values != null)
                    hashCode = hashCode * 59 + this.Values.GetHashCode();
                hashCode = hashCode * 59 + this.Type.GetHashCode();
                return hashCode;
            }
        }

    }

}

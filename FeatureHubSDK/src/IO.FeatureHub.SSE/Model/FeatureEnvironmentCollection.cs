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
    /// This represents a collection of features as per a request from a GET api. GET&#39;s can request multiple API Keys at the same time.
    /// </summary>
    [DataContract(Name = "FeatureEnvironmentCollection")]
    public partial class FeatureEnvironmentCollection : IEquatable<FeatureEnvironmentCollection>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureEnvironmentCollection" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected FeatureEnvironmentCollection() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureEnvironmentCollection" /> class.
        /// </summary>
        /// <param name="id">id (required).</param>
        /// <param name="features">features.</param>
        public FeatureEnvironmentCollection(Guid id = default(Guid), List<FeatureState> features = default(List<FeatureState>))
        {
            this.Id = id;
            this.Features = features;
        }

        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name = "id", IsRequired = true, EmitDefaultValue = false)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or Sets Features
        /// </summary>
        [DataMember(Name = "features", EmitDefaultValue = false)]
        public List<FeatureState> Features { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class FeatureEnvironmentCollection {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Features: ").Append(Features).Append("\n");
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
            return this.Equals(input as FeatureEnvironmentCollection);
        }

        /// <summary>
        /// Returns true if FeatureEnvironmentCollection instances are equal
        /// </summary>
        /// <param name="input">Instance of FeatureEnvironmentCollection to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(FeatureEnvironmentCollection input)
        {
            if (input == null)
            {
                return false;
            }
            return 
                (
                    this.Id == input.Id ||
                    (this.Id != null &&
                    this.Id.Equals(input.Id))
                ) && 
                (
                    this.Features == input.Features ||
                    this.Features != null &&
                    input.Features != null &&
                    this.Features.SequenceEqual(input.Features)
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
                {
                    hashCode = (hashCode * 59) + this.Id.GetHashCode();
                }
                if (this.Features != null)
                {
                    hashCode = (hashCode * 59) + this.Features.GetHashCode();
                }
                return hashCode;
            }
        }

    }

}
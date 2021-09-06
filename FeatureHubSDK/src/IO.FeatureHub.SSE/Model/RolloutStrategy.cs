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
    /// if the feature in an environment is different from its default, this will be the reason for it. a rollout strategy is defined at the Application level and then applied to a specific feature value. When they are copied to the cache layer they are cloned and the feature value for that strategy is inserted into the clone and those are published.
    /// </summary>
    [DataContract(Name = "RolloutStrategy")]
    public partial class RolloutStrategy : IEquatable<RolloutStrategy>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RolloutStrategy" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected RolloutStrategy() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="RolloutStrategy" /> class.
        /// </summary>
        /// <param name="id">id.</param>
        /// <param name="name">names are unique in a case insensitive fashion (required).</param>
        /// <param name="percentage">value between 0 and 1000000 - for four decimal places.</param>
        /// <param name="percentageAttributes">if you don&#39;t wish to apply percentage based on user id, you can use one or more attributes defined here.</param>
        /// <param name="colouring">the colour used to display the strategy in the UI. indexed table of background/foreground combos..</param>
        /// <param name="avatar">url to avatar (if any). Not sent to SDK. Preferably a unicorn..</param>
        /// <param name="value">when we attach the RolloutStrategy for Dacha or SSE this lets us push the value out. Only visible in SDK and SSE Edge..</param>
        /// <param name="attributes">attributes.</param>
        public RolloutStrategy(string id = default(string), string name = default(string), int percentage = default(int), List<string> percentageAttributes = default(List<string>), int colouring = default(int), string avatar = default(string), object value = default(object), List<RolloutStrategyAttribute> attributes = default(List<RolloutStrategyAttribute>))
        {
            // to ensure "name" is required (not null)
            this.Name = name ?? throw new ArgumentNullException("name is a required property for RolloutStrategy and cannot be null");
            this.Id = id;
            this.Percentage = percentage;
            this.PercentageAttributes = percentageAttributes;
            this.Colouring = colouring;
            this.Avatar = avatar;
            this.Value = value;
            this.Attributes = attributes;
        }

        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// names are unique in a case insensitive fashion
        /// </summary>
        /// <value>names are unique in a case insensitive fashion</value>
        [DataMember(Name = "name", IsRequired = true, EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// value between 0 and 1000000 - for four decimal places
        /// </summary>
        /// <value>value between 0 and 1000000 - for four decimal places</value>
        [DataMember(Name = "percentage", EmitDefaultValue = false)]
        public int Percentage { get; set; }

        /// <summary>
        /// if you don&#39;t wish to apply percentage based on user id, you can use one or more attributes defined here
        /// </summary>
        /// <value>if you don&#39;t wish to apply percentage based on user id, you can use one or more attributes defined here</value>
        [DataMember(Name = "percentageAttributes", EmitDefaultValue = false)]
        public List<string> PercentageAttributes { get; set; }

        /// <summary>
        /// the colour used to display the strategy in the UI. indexed table of background/foreground combos.
        /// </summary>
        /// <value>the colour used to display the strategy in the UI. indexed table of background/foreground combos.</value>
        [DataMember(Name = "colouring", EmitDefaultValue = false)]
        public int Colouring { get; set; }

        /// <summary>
        /// url to avatar (if any). Not sent to SDK. Preferably a unicorn.
        /// </summary>
        /// <value>url to avatar (if any). Not sent to SDK. Preferably a unicorn.</value>
        [DataMember(Name = "avatar", EmitDefaultValue = false)]
        public string Avatar { get; set; }

        /// <summary>
        /// when we attach the RolloutStrategy for Dacha or SSE this lets us push the value out. Only visible in SDK and SSE Edge.
        /// </summary>
        /// <value>when we attach the RolloutStrategy for Dacha or SSE this lets us push the value out. Only visible in SDK and SSE Edge.</value>
        [DataMember(Name = "value", EmitDefaultValue = true)]
        public object Value { get; set; }

        /// <summary>
        /// Gets or Sets Attributes
        /// </summary>
        [DataMember(Name = "attributes", EmitDefaultValue = false)]
        public List<RolloutStrategyAttribute> Attributes { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class RolloutStrategy {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Name: ").Append(Name).Append("\n");
            sb.Append("  Percentage: ").Append(Percentage).Append("\n");
            sb.Append("  PercentageAttributes: ").Append(PercentageAttributes).Append("\n");
            sb.Append("  Colouring: ").Append(Colouring).Append("\n");
            sb.Append("  Avatar: ").Append(Avatar).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("  Attributes: ").Append(Attributes).Append("\n");
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
            return this.Equals(input as RolloutStrategy);
        }

        /// <summary>
        /// Returns true if RolloutStrategy instances are equal
        /// </summary>
        /// <param name="input">Instance of RolloutStrategy to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(RolloutStrategy input)
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
                    this.Name == input.Name ||
                    (this.Name != null &&
                    this.Name.Equals(input.Name))
                ) && 
                (
                    this.Percentage == input.Percentage ||
                    this.Percentage.Equals(input.Percentage)
                ) && 
                (
                    this.PercentageAttributes == input.PercentageAttributes ||
                    this.PercentageAttributes != null &&
                    input.PercentageAttributes != null &&
                    this.PercentageAttributes.SequenceEqual(input.PercentageAttributes)
                ) && 
                (
                    this.Colouring == input.Colouring ||
                    this.Colouring.Equals(input.Colouring)
                ) && 
                (
                    this.Avatar == input.Avatar ||
                    (this.Avatar != null &&
                    this.Avatar.Equals(input.Avatar))
                ) && 
                (
                    this.Value == input.Value ||
                    (this.Value != null &&
                    this.Value.Equals(input.Value))
                ) && 
                (
                    this.Attributes == input.Attributes ||
                    this.Attributes != null &&
                    input.Attributes != null &&
                    this.Attributes.SequenceEqual(input.Attributes)
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
                if (this.Name != null)
                    hashCode = hashCode * 59 + this.Name.GetHashCode();
                hashCode = hashCode * 59 + this.Percentage.GetHashCode();
                if (this.PercentageAttributes != null)
                    hashCode = hashCode * 59 + this.PercentageAttributes.GetHashCode();
                hashCode = hashCode * 59 + this.Colouring.GetHashCode();
                if (this.Avatar != null)
                    hashCode = hashCode * 59 + this.Avatar.GetHashCode();
                if (this.Value != null)
                    hashCode = hashCode * 59 + this.Value.GetHashCode();
                if (this.Attributes != null)
                    hashCode = hashCode * 59 + this.Attributes.GetHashCode();
                return hashCode;
            }
        }

    }

}

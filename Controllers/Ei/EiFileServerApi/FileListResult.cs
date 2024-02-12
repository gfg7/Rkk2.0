/*
 * File Service API
 *
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * The version of the OpenAPI document: 1.0
 * 
 * Generated by: https://openapi-generator.tech
 */

using System;
using System.Text;
using System.Runtime.Serialization;

namespace Rkk2._0.Controllers.Ei
{

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public partial class FileListResult : IEquatable<FileListResult>
    {
        /// <summary>
        /// Gets or Sets TotalCount
        /// </summary>
        [DataMember(Name="totalCount", EmitDefaultValue=true)]
        public long TotalCount { get; set; }

        /// <summary>
        /// Gets or Sets Page
        /// </summary>
        [DataMember(Name="page", EmitDefaultValue=false)]
        public FileListResultPage Page { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class FileListResult {\n");
            sb.Append("  TotalCount: ").Append(TotalCount).Append("\n");
            sb.Append("  Page: ").Append(Page).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }


        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="obj">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((FileListResult)obj);
        }

        /// <summary>
        /// Returns true if FileListResult instances are equal
        /// </summary>
        /// <param name="other">Instance of FileListResult to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(FileListResult other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return 
                (
                    TotalCount == other.TotalCount ||
                    
                    TotalCount.Equals(other.TotalCount)
                ) && 
                (
                    Page == other.Page ||
                    Page != null &&
                    Page.Equals(other.Page)
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
                var hashCode = 41;
                // Suitable nullity checks etc, of course :)
                    
                    hashCode = hashCode * 59 + TotalCount.GetHashCode();
                    if (Page != null)
                    hashCode = hashCode * 59 + Page.GetHashCode();
                return hashCode;
            }
        }

        #region Operators
        #pragma warning disable 1591

        public static bool operator ==(FileListResult left, FileListResult right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FileListResult left, FileListResult right)
        {
            return !Equals(left, right);
        }

        #pragma warning restore 1591
        #endregion Operators
    }
}

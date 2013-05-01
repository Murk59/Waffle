﻿//// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Reflection;
//using System.Security;
//using System.Web.Http.Metadata;
//using System.Web.Http.Properties;
//using System.Web.Http.Validation.Validators;

//namespace System.Web.Http.Validation.Providers
//{
//    using CommandProcessing.Validation;
//    using CommandProcessing.Validation.Providers;

//    /// <summary>
//    /// An implementation of <see cref="ModelValidatorProvider"/> which provides validators that throw exceptions when the model is invalid.
//    /// </summary>
//    public class InvalidModelValidatorProvider : AssociatedValidatorProvider
//    {
//        protected override IEnumerable<ModelValidator> GetValidators(ModelMetadata metadata, IEnumerable<ModelValidatorProvider> validatorProviders, IEnumerable<Attribute> attributes)
//        {
//            if (metadata.ContainerType == null || String.IsNullOrEmpty(metadata.PropertyName))
//            {
//                // Validate that the type's fields and nonpublic properties don't have any validation attributes on them
//                // Validation only runs against public properties
//                Type type = metadata.ModelType;
//                PropertyInfo[] nonPublicProperties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
//                foreach (PropertyInfo nonPublicProperty in nonPublicProperties)
//                {
//                    if (nonPublicProperty.GetCustomAttributes(typeof(ValidationAttribute), inherit: true).Length > 0)
//                    {
//                        yield return new ErrorModelValidator(validatorProviders, Error.Format(Resources.ValidationAttributeOnNonPublicProperty, nonPublicProperty.Name, type));
//                    }
//                }

//                FieldInfo[] allFields = metadata.ModelType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//                foreach (FieldInfo field in allFields)
//                {
//                    if (field.GetCustomAttributes(typeof(ValidationAttribute), inherit: true).Length > 0)
//                    {
//                        yield return new ErrorModelValidator(validatorProviders, Error.Format(Resources.ValidationAttributeOnField, field.Name, type));
//                    }
//                }
//            }
//            else
//            {
//                // Validate that value-typed properties marked as [Required] are also marked as [DataMember(IsRequired=true)]
//                // Certain formatters may not recognize a member as required if it's marked as [Required] but not [DataMember(IsRequired=true)]
//                // This is not a problem for reference types because [Required] will still cause a model error to be raised after a null value is deserialized
//                if (metadata.ModelType.IsValueType && attributes.Any(IsRequiredAttribute))
//                {
//                    if (!DataMemberModelValidatorProvider.IsRequiredDataMember(metadata.ContainerType, attributes))
//                    {
//                        yield return new ErrorModelValidator(validatorProviders, Error.Format(Resources.MissingDataMemberIsRequired, metadata.PropertyName, metadata.ContainerType));
//                    }
//                }
//            }
//        }

//        // SecurityCritical: uses DataAnnotations type RequiredAttribute
//        // SecuritySafe: RequiredAttribute is actually a safe type, mistakenly critical in 4.0
//        [SecuritySafeCritical]
//        private static bool IsRequiredAttribute(Attribute attribute)
//        {
//            return attribute is RequiredAttribute;
//        }
//    }
//}

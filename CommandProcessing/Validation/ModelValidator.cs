﻿namespace CommandProcessing.Validation
{
    using System.Collections.Generic;
    using CommandProcessing.Internal;
    using CommandProcessing.Metadata;

    public abstract class ModelValidator
    {
        protected ModelValidator(IEnumerable<ModelValidatorProvider> validatorProviders)
        {
            if (validatorProviders == null)
            {
                throw Error.ArgumentNull("validatorProviders");
            }

            ValidatorProviders = validatorProviders;
        }

        protected internal IEnumerable<ModelValidatorProvider> ValidatorProviders { get; private set; }

        public virtual bool IsRequired
        {
            get { return false; }
        }

        public static ModelValidator GetModelValidator(IEnumerable<ModelValidatorProvider> validatorProviders)
        {
            return new CompositeModelValidator(validatorProviders);
        }

        public abstract IEnumerable<ModelValidationResult> Validate(ModelMetadata metadata, object container);

        private class CompositeModelValidator : ModelValidator
        {
            public CompositeModelValidator(IEnumerable<ModelValidatorProvider> validatorProviders)
                : base(validatorProviders)
            {
            }

            public override IEnumerable<ModelValidationResult> Validate(ModelMetadata metadata, object container)
            {
                bool propertiesValid = true;

                foreach (ModelMetadata propertyMetadata in metadata.Properties)
                {
                    foreach (ModelValidator propertyValidator in propertyMetadata.GetValidators(this.ValidatorProviders))
                    {
                        foreach (ModelValidationResult propertyResult in propertyValidator.Validate(metadata, container))
                        {
                            propertiesValid = false;
                            yield return new ModelValidationResult
                            {
                                MemberName = CreatePropertyModelName(propertyMetadata.PropertyName, propertyResult.MemberName),
                                Message = propertyResult.Message
                            };
                        }
                    }
                }

                if (propertiesValid)
                {
                    foreach (ModelValidator typeValidator in metadata.GetValidators(this.ValidatorProviders))
                    {
                        foreach (ModelValidationResult typeResult in typeValidator.Validate(metadata, container))
                        {
                            yield return typeResult;
                        }
                    }
                }
            }
        }

        private static string CreatePropertyModelName(string prefix, string propertyName)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return propertyName ?? string.Empty;
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                return prefix;
            }

            return prefix + "." + propertyName;
        }
    }

}

using ServiceStack.FluentValidation;

namespace ServiceStack.Serilog.RequestLogsFeature.Plugin
{
    public class FeatureValidator : AbstractValidator<Feature>
    {
        public FeatureValidator()
        {
            RuleFor(feat => feat.RequestLogger)
                .NotNull()
                ;
        }
    }
}

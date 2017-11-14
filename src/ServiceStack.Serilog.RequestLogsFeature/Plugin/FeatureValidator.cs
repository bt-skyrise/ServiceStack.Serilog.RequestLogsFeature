using ServiceStack.FluentValidation;

namespace ServiceStack.Serilog.RequestLogsFeature.Plugin
{
    public class FeatureValidator : AbstractValidator<SerilogRequestLogsFeature>
    {
        public FeatureValidator()
        {
            RuleFor(feat => feat.RequestLogger)
                .NotNull()
                ;
        }
    }
}

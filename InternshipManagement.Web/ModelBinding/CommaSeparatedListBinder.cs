using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace InternshipManagement.Web.ModelBinding
{
    /// <summary>
    /// The "Eligible programmes" box is one text field ("Computer Science, Engineering"), but the API wants a list.
    /// Without this the whole text arrives as a single list item.
    /// </summary>
    public sealed class CommaSeparatedListBinder : IModelBinder
    {
        private static readonly char[] Separators = { ',', ';', '\n', '\r' };

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (value == ValueProviderResult.None)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
                return Task.CompletedTask;
            }

            var items = value.Values
                .SelectMany(v => (v ?? string.Empty).Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(s => s.Length > 100 ? s[..100] : s)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(30)
                .ToList();

            bindingContext.Result = ModelBindingResult.Success(items.Count == 0 ? null : items);
            return Task.CompletedTask;
        }
    }
}

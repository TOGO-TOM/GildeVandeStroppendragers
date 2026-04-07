using System;
using System.IO;

namespace AdminMembers
{
    // This class is used as a marker for localized resources
    // The actual translations are in Resources/SharedResources.{culture}.resx files
    public class SharedResources
    {
        private static readonly Lazy<string> _svgSprite = new(() =>
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "icons.svg");
            return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
        });

        public static string SvgSprite => _svgSprite.Value;
    }
}

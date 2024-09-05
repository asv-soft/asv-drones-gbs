using System.Reflection;

namespace Asv.Drones.Gbs;

/// <summary>
/// Extension methods for retrieving assembly information.
/// </summary>
public static class AssemblyInfoExt
    {
        /// <summary>
        /// Retrieves the version of the specified assembly.
        /// </summary>
        /// <param name="src">The assembly for which to retrieve the version.</param>
        /// <returns>The version of the specified assembly.</returns>
        public static Version GetVersion(this Assembly src)
        {
            return src.GetName().Version;
        }

        /// <summary>
        /// Retrieves the informational version of the specified assembly.
        /// </summary>
        /// <param name="src">The assembly from which to retrieve the informational version.</param>
        /// <returns>The informational version of the specified assembly. Returns an empty string if no informational version is found.</returns>
        public static string GetInformationalVersion(this Assembly src)
        {
            var attributes = src.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
        }

        /// <summary>
        /// Retrieves the title of an assembly.
        /// </summary>
        /// <param name="src">The assembly to retrieve the title from.</param>
        /// <returns>The title of the assembly. If the assembly has an <see cref="AssemblyTitleAttribute"/> defined, the title from the attribute is returned. If not, the file name of the assembly without the extension is returned.</returns>
        public static string GetTitle(this Assembly src)
        {
            var attributes = src.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                var titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (titleAttribute.Title.Length > 0) return titleAttribute.Title;
            }
            return System.IO.Path.GetFileNameWithoutExtension(src.CodeBase);
        }

        /// <summary>
        /// Gets the product name of the specified assembly.
        /// </summary>
        /// <param name="src">The assembly for which to retrieve the product name.</param>
        /// <returns>The product name of the assembly.</returns>
        public static string GetProductName(this Assembly src)
        {
            var attributes = src.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyProductAttribute)attributes[0]).Product;
        }

        /// <summary>
        /// Retrieves the description of the specified assembly.
        /// </summary>
        /// <param name="src">The assembly for which to retrieve the description.</param>
        /// <returns>The description of the specified assembly. If no description is found, an empty string is returned.</returns>
        public static string GetDescription(this Assembly src)
        {
            var attributes = src.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyDescriptionAttribute) attributes[0]).Description;
        }

        /// <summary>
        /// Retrieves the copyright holder for the specified assembly.
        /// </summary>
        /// <param name="src">The assembly from which to retrieve the copyright holder.</param>
        /// <returns>The copyright holder of the assembly. Returns an empty string if the assembly has no copyright holder.</returns>
        public static string GetCopyrightHolder(this Assembly src)
        {
            var attributes = src.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyCopyrightAttribute) attributes[0]).Copyright;
        }

        /// <summary>
        /// Retrieves the company name specified in the AssemblyCompany attribute of the assembly.
        /// </summary>
        /// <param name="src">The assembly from which to retrieve the company name.</param>
        /// <returns>
        /// The company name specified in the AssemblyCompany attribute of the assembly.
        /// If the attribute is not found or the company name is not specified, an empty string is returned.
        /// </returns>
        public static string GetCompanyName(this Assembly src)
        {
            var attributes = src.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyCompanyAttribute)attributes[0]).Company;
        }
    }
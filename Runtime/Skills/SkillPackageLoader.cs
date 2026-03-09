using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Discovers and parses standard Claude SKILL packages from the file-system.
    ///
    /// A skill package is any directory that contains a <c>SKILL.md</c> file.  The file must
    /// begin with a YAML frontmatter block enclosed between two <c>---</c> lines.  Supported
    /// frontmatter keys are:
    /// <list type="bullet">
    ///   <item><c>name</c> – skill identifier (required)</item>
    ///   <item><c>description</c> – short summary</item>
    ///   <item><c>version</c> – semantic version</item>
    ///   <item><c>author</c> – author name</item>
    ///   <item><c>allowed-tools</c> – YAML list of tool names</item>
    ///   <item>Any other key – stored in <see cref="SkillPackage.Metadata"/></item>
    /// </list>
    /// Everything after the closing <c>---</c> is stored as <see cref="SkillPackage.Instructions"/>.
    /// </summary>
    public static class SkillPackageLoader
    {
        private const string SkillFileName = "SKILL.md";

        // ------------------------------------------------------------------ public API

        /// <summary>
        /// Recursively walks <paramref name="rootDirectory"/> and returns a
        /// <see cref="SkillPackage"/> for every directory that contains a <c>SKILL.md</c> file.
        /// </summary>
        public static List<SkillPackage> DiscoverSkillPackages(string rootDirectory)
        {
            var packages = new List<SkillPackage>();

            if (string.IsNullOrEmpty(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                Debug.LogWarning($"[SkillPackageLoader] Directory not found: {rootDirectory}");
                return packages;
            }

            var skillFiles = Directory.GetFiles(rootDirectory, SkillFileName, SearchOption.AllDirectories);

            foreach (var skillFile in skillFiles)
            {
                try
                {
                    var package = ParseSkillFile(skillFile);
                    if (package != null)
                    {
                        packages.Add(package);
                        Debug.Log($"[SkillPackageLoader] Loaded skill package: {package.Name} from {package.SourcePath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SkillPackageLoader] Error loading '{skillFile}': {ex.Message}");
                }
            }

            Debug.Log($"[SkillPackageLoader] Discovered {packages.Count} skill package(s) under: {rootDirectory}");
            return packages;
        }

        // ------------------------------------------------------------------ parsing

        /// <summary>Parse a single SKILL.md file and return the resulting <see cref="SkillPackage"/>.</summary>
        public static SkillPackage ParseSkillFile(string skillFilePath)
        {
            if (!File.Exists(skillFilePath))
            {
                Debug.LogWarning($"[SkillPackageLoader] SKILL.md not found: {skillFilePath}");
                return null;
            }

            var content = File.ReadAllText(skillFilePath);
            var package = new SkillPackage
            {
                SourcePath = Path.GetDirectoryName(skillFilePath)
            };

            ParseContent(content, package);

            if (string.IsNullOrEmpty(package.Name))
            {
                // Fall back to directory name when no name was declared in frontmatter
                package.Name = Path.GetFileName(package.SourcePath);
                Debug.LogWarning($"[SkillPackageLoader] No 'name' in frontmatter of '{skillFilePath}'; using directory name '{package.Name}'.");
            }

            return package;
        }

        // ------------------------------------------------------------------ private helpers

        private static void ParseContent(string content, SkillPackage package)
        {
            // Normalise line endings
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");

            // The frontmatter block must start at the very beginning of the file
            if (!content.StartsWith("---"))
            {
                // No frontmatter – treat entire file as instructions
                package.Instructions = content.Trim();
                return;
            }

            // Find the closing --- (must be on its own line)
            var closingIndex = content.IndexOf("\n---", 3);
            if (closingIndex < 0)
            {
                // Malformed – treat entire file as instructions
                package.Instructions = content.Trim();
                return;
            }

            // Ensure the closing --- is followed by a newline or end-of-content (not mid-line)
            var afterDashes = closingIndex + 4; // skip \n---
            if (afterDashes < content.Length && content[afterDashes] != '\n' && content[afterDashes] != '\r')
            {
                // The --- is part of something else (e.g. a markdown heading separator) – keep searching
                var nextAttempt = content.IndexOf("\n---", afterDashes);
                if (nextAttempt < 0)
                {
                    package.Instructions = content.Trim();
                    return;
                }
                closingIndex = nextAttempt;
                afterDashes = closingIndex + 4;
            }

            var frontmatter = content.Substring(3, closingIndex - 3).Trim();
            var body = afterDashes < content.Length ? content.Substring(afterDashes).TrimStart('\n') : string.Empty;
            package.Instructions = body.Trim();

            ParseFrontmatter(frontmatter, package);
        }

        /// <summary>
        /// Minimal YAML parser that handles the subset of YAML used in SKILL.md frontmatter:
        /// scalar key/value pairs and sequence (list) values introduced by a bare key followed
        /// by lines starting with "  - ".
        /// </summary>
        private static void ParseFrontmatter(string frontmatter, SkillPackage package)
        {
            var lines = frontmatter.Split('\n');

            string currentKey = null;
            bool inList = false;
            var currentList = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var raw = lines[i];
                var trimmed = raw.Trim();

                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // List item under the current key (indented by at least one space or tab)
                if (inList && raw.Length > 0 && (raw[0] == ' ' || raw[0] == '\t') && trimmed.StartsWith("- "))
                {
                    var item = trimmed.Substring(2).Trim();
                    // Strip optional inline YAML quotes
                    item = StripYamlQuotes(item);
                    currentList.Add(item);
                    continue;
                }

                // Leaving a list – commit it
                if (inList)
                {
                    CommitList(currentKey, currentList, package);
                    currentList = new List<string>();
                    inList = false;
                    currentKey = null;
                }

                // Key: value  or  key:
                var colonIndex = trimmed.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                var key = trimmed.Substring(0, colonIndex).Trim().ToLowerInvariant();
                var value = trimmed.Substring(colonIndex + 1).Trim();
                value = StripYamlQuotes(value);

                if (string.IsNullOrEmpty(value))
                {
                    // Potentially a list follows
                    currentKey = key;
                    inList = true;
                    continue;
                }

                // Scalar value
                ApplyScalar(key, value, package);
            }

            // Commit any pending list
            if (inList && currentList.Count > 0)
                CommitList(currentKey, currentList, package);
        }

        private static void ApplyScalar(string key, string value, SkillPackage package)
        {
            switch (key)
            {
                case "name":        package.Name        = value; break;
                case "description": package.Description = value; break;
                case "version":     package.Version     = value; break;
                case "author":      package.Author      = value; break;
                default:
                    package.Metadata[key] = value;
                    break;
            }
        }

        private static void CommitList(string key, List<string> items, SkillPackage package)
        {
            if (key == null || items == null || items.Count == 0)
                return;

            switch (key)
            {
                case "allowed-tools":
                    package.AllowedTools.AddRange(items);
                    break;
                default:
                    // Store comma-separated list in metadata
                    package.Metadata[key] = string.Join(", ", items);
                    break;
            }
        }

        private static string StripYamlQuotes(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
            {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
    }
}

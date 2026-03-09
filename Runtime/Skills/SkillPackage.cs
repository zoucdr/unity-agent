using System.Collections.Generic;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// Represents a standard Claude SKILL package discovered from a directory containing SKILL.md.
    /// The SKILL.md file must begin with a YAML frontmatter block (delimited by ---) that provides
    /// metadata such as name, description, version, allowed-tools, and arbitrary key/value pairs.
    /// The remainder of the file is stored as the skill's usage instructions.
    /// </summary>
    [System.Serializable]
    public class SkillPackage
    {
        // ------------------------------------------------------------------ frontmatter fields

        /// <summary>Skill identifier from the <c>name</c> frontmatter field.</summary>
        public string Name;

        /// <summary>Short human-readable summary from the <c>description</c> frontmatter field.</summary>
        public string Description;

        /// <summary>Semantic version string from the <c>version</c> frontmatter field (optional).</summary>
        public string Version;

        /// <summary>Author from the <c>author</c> frontmatter field (optional).</summary>
        public string Author;

        /// <summary>Tools this skill is permitted to use, from <c>allowed-tools</c> frontmatter field.</summary>
        public List<string> AllowedTools = new List<string>();

        /// <summary>Arbitrary key/value metadata pairs from the frontmatter block.</summary>
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();

        // ------------------------------------------------------------------ body

        /// <summary>Full body text from SKILL.md (everything after the closing --- of the frontmatter).</summary>
        public string Instructions;

        // ------------------------------------------------------------------ filesystem

        /// <summary>Absolute path to the directory that contains this skill's SKILL.md.</summary>
        public string SourcePath;

        // ------------------------------------------------------------------ runtime state

        /// <summary>Whether this skill package is currently enabled for use.</summary>
        public bool Enabled = true;

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Returns a concise description of this skill suitable for inclusion in an LLM prompt.
        /// </summary>
        public string GetCapabilityDescription()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Name);
            if (!string.IsNullOrEmpty(Description))
                sb.Append(": ").Append(Description);
            if (AllowedTools != null && AllowedTools.Count > 0)
                sb.Append(" [tools: ").Append(string.Join(", ", AllowedTools)).Append("]");
            return sb.ToString();
        }

        public override string ToString() => $"SkillPackage({Name}, v{Version}, enabled={Enabled})";
    }
}

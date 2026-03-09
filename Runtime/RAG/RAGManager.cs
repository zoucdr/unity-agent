using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Unity3DAgent.Core
{
    /// <summary>
    /// RAG (Retrieval-Augmented Generation) manager.
    /// Indexes Markdown / plain-text documents and retrieves the most relevant
    /// chunks for a given query using TF-IDF scoring.
    /// </summary>
    public class RAGManager : MonoBehaviour
    {
        [SerializeField] private AIAgentConfig config;

        private readonly DocumentIndex documentIndex = new DocumentIndex();

        public DocumentIndex Index => documentIndex;

        /// <summary>Total number of indexed chunks.</summary>
        public int ChunkCount => documentIndex.ChunkCount;

        private void Start()
        {
            if (config?.RAGConfig != null && config.RAGConfig.Enabled && config.RAGConfig.AutoLoadDocuments)
            {
                LoadDocumentsFromConfig();
            }

            // Apply configurable chunking parameters
            if (config?.RAGConfig != null)
            {
                documentIndex.SetChunkParameters(config.RAGConfig.ChunkSize, config.RAGConfig.ChunkOverlap);
            }
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Load and index all documents listed in the agent config.</summary>
        public void LoadDocumentsFromConfig()
        {
            if (config?.RAGConfig == null) return;

            foreach (var path in config.RAGConfig.DocumentPaths)
            {
                LoadDocumentsFromPath(path);
            }
        }

        /// <summary>
        /// Load and index every .md / .txt file found at <paramref name="path"/>.
        /// <paramref name="path"/> may point to a single file or a directory.
        /// </summary>
        public void LoadDocumentsFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            if (File.Exists(path))
            {
                LoadDocument(path);
            }
            else if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.md", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories));

                foreach (var file in files)
                {
                    LoadDocument(file);
                }
            }
            else
            {
                Debug.LogWarning($"[RAGManager] Path not found: {path}");
            }
        }

        /// <summary>Load and index a single file.</summary>
        public void LoadDocument(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath, Encoding.UTF8);
                var name = Path.GetFileName(filePath);
                documentIndex.AddDocument(name, filePath, content);
                Debug.Log($"[RAGManager] Indexed document: {name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RAGManager] Error loading '{filePath}': {ex.Message}");
            }
        }

        /// <summary>Add document content directly (not from disk).</summary>
        public void AddDocument(string name, string content)
        {
            documentIndex.AddDocument(name, name, content);
        }

        /// <summary>
        /// Retrieve the top-K most relevant chunks for <paramref name="query"/>.
        /// </summary>
        public List<DocumentChunk> Retrieve(string query, int topK = -1)
        {
            int k = topK > 0 ? topK : (config?.RAGConfig?.TopKResults ?? 3);
            return documentIndex.Search(query, k);
        }

        /// <summary>
        /// Build a formatted context string from retrieved document chunks,
        /// ready to prepend to an LLM prompt.
        /// </summary>
        public string BuildContext(string query, int topK = -1)
        {
            var chunks = Retrieve(query, topK);
            if (chunks.Count == 0) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("Relevant context from knowledge base:");

            for (int i = 0; i < chunks.Count; i++)
            {
                sb.AppendLine($"\n[Source {i + 1}: {chunks[i].DocumentName}]");
                sb.AppendLine(chunks[i].Content);
            }

            return sb.ToString();
        }

        /// <summary>Remove all indexed documents.</summary>
        public void Clear()
        {
            documentIndex.Clear();
            Debug.Log("[RAGManager] Document index cleared");
        }
    }

    // ===================================================================== model types

    /// <summary>A single indexed passage from a document.</summary>
    public class DocumentChunk
    {
        public string DocumentName;
        public string FilePath;
        public string Content;
        public float Score;
        public int ChunkIndex;
    }

    /// <summary>
    /// In-memory TF-IDF document index supporting chunking and keyword search.
    /// </summary>
    public class DocumentIndex
    {
        private readonly List<DocumentChunk> chunks = new List<DocumentChunk>();

        // Defaults; may be overridden via SetChunkParameters
        private int chunkSize = 500;
        private int chunkOverlap = 100;

        /// <summary>Override the default chunk size and overlap.</summary>
        public void SetChunkParameters(int size, int overlap)
        {
            chunkSize = Math.Max(1, size);
            chunkOverlap = Math.Max(0, Math.Min(overlap, chunkSize - 1));
        }

        // ------------------------------------------------------------------ public API

        /// <summary>Add and chunk a document into the index.</summary>
        public void AddDocument(string name, string path, string content)
        {
            var newChunks = ChunkText(content, name, path);
            chunks.AddRange(newChunks);
        }

        /// <summary>
        /// Search for the <paramref name="topK"/> most relevant chunks for
        /// <paramref name="query"/>. Returns only chunks with a score > 0.
        /// </summary>
        public List<DocumentChunk> Search(string query, int topK = 3)
        {
            if (chunks.Count == 0) return new List<DocumentChunk>();

            var queryTerms = Tokenize(query);

            return chunks
                .Select(c => { c.Score = ComputeScore(c.Content, queryTerms); return c; })
                .Where(c => c.Score > 0)
                .OrderByDescending(c => c.Score)
                .Take(topK)
                .ToList();
        }

        /// <summary>Remove all chunks.</summary>
        public void Clear() => chunks.Clear();

        /// <summary>Total number of indexed chunks.</summary>
        public int ChunkCount => chunks.Count;

        // ------------------------------------------------------------------ helpers

        private List<DocumentChunk> ChunkText(string text, string name, string path)
        {
            var result = new List<DocumentChunk>();
            var words = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            int step = Math.Max(1, chunkSize - chunkOverlap); // guarantee forward progress
            int i = 0;
            int chunkIdx = 0;

            while (i < words.Length)
            {
                var chunkWords = words.Skip(i).Take(chunkSize).ToArray();
                result.Add(new DocumentChunk
                {
                    DocumentName = name,
                    FilePath = path,
                    Content = string.Join(" ", chunkWords),
                    ChunkIndex = chunkIdx++
                });

                i += step;
            }

            return result;
        }

        private static string[] Tokenize(string text)
        {
            return text.ToLower()
                .Split(new[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':', '"', '\'', '(', ')' },
                    StringSplitOptions.RemoveEmptyEntries);
        }

        private static float ComputeScore(string chunkContent, string[] queryTerms)
        {
            var chunkTerms = Tokenize(chunkContent);
            if (chunkTerms.Length == 0) return 0f;

            // Build term-frequency map for the chunk
            var tf = new Dictionary<string, int>(chunkTerms.Length);
            foreach (var t in chunkTerms)
            {
                if (!tf.ContainsKey(t)) tf[t] = 0;
                tf[t]++;
            }

            float score = 0f;
            foreach (var term in queryTerms)
            {
                if (tf.TryGetValue(term, out var freq))
                    score += (float)freq / chunkTerms.Length;
            }

            return score;
        }
    }
}

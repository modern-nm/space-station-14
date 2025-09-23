using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client.UserInterface.Systems.Chat
{
    internal class ChatAutocompleteHandler
    {
        public Trie Lexicon { get; set; }
        public JsonSerializerOptions Options { get; set; }
        private string JsonString { get; set; }

        public string PrevInput { get; set; }

        readonly string _dictpath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            @"Space Station 14/data/autocomplete_dict.json");

        public ChatAutocompleteHandler()
        {
            Lexicon = new Trie();
            JsonString = "";
            Options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                WriteIndented = true
            };
            PrevInput = "";
            LoadDictionary();
            SaveDictionary();
        }

        private void LoadDictionary()
        {
            if (System.OperatingSystem.IsWindows())
            {
                if (!File.Exists(_dictpath))
                {
                    SaveDictionary();
                }
                else
                {
                    JsonString = File.ReadAllText(_dictpath);
                    var data = JsonSerializer.Deserialize<List<Word>>(JsonString, Options);
                    if (data != null)
                    {
                        foreach (var w in data)
                            Lexicon.Insert(w.Text, w.Frequency);
                    }
                }
            }
        }

        public void SaveDictionary()
        {
            var words = Lexicon.ToWordList();
            var str = JsonSerializer.Serialize(words, Options);
            File.WriteAllText(_dictpath, str);
        }

        public void AddWord(string item)
        {
            Lexicon.Insert(item);
        }

        public List<string> ParseInput(string input)
        {
            Regex regex = new Regex(@"\w+");
            MatchCollection words = regex.Matches(input);
            List<string> strings = new List<string>();
            foreach (Match word in words)
            {
                strings.Add(word.Value.ToLower());
            }
            return strings;
        }

        public string GetWordPartToAppend(string input)
        {
            Regex regex = new Regex(@"\w+$");
            Match match = regex.Match(input);
            if (match.Value != "")
            {
                var suggestions = Lexicon.GetSuggestions(match.Value.ToLower(), 1);
                if (suggestions.Count > 0)
                {
                    var suggestion = suggestions[0];
                    if (PrevInput.Length >= input.Length)
                    {
                        PrevInput = input;
                        return "";
                    }

                    PrevInput = input;
                    return suggestion.Substring(match.Value.Length);
                }
            }
            PrevInput = input;
            return "";
        }

        public string GetLastWord(string text)
        {
            int idx = text.LastIndexOf(' ');
            return idx == -1 ? text : text.Substring(idx + 1);
        }

        public string ReplaceLastWord(string text, string replacement)
        {
            int idx = text.LastIndexOf(' ');
            if (idx == -1)
                return replacement;
            return text.Substring(0, idx + 1) + replacement;
        }

        public List<string> GetCompletions(string input)
        {
            Regex regex = new Regex(@"\w+$");
            Match match = regex.Match(input);
            if (match.Success)
                return Lexicon.GetSuggestions(match.Value.ToLower(), 10);
            return new List<string>();
        }
    }

    internal class Word
    {
        public string Text { get; set; }
        public int Frequency { get; set; }
        public Word(string text, int frequency = 1)
        {
            Text = text;
            Frequency = frequency;
        }
    }

    internal class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = new();
        public bool IsWord { get; set; }
        public int Frequency { get; set; }
    }

    internal class Trie
    {
        private readonly TrieNode _root = new();

        public void Insert(string word, int frequency = 1)
        {
            var node = _root;
            foreach (var ch in word)
            {
                if (!node.Children.ContainsKey(ch))
                    node.Children[ch] = new TrieNode();
                node = node.Children[ch];
            }
            node.IsWord = true;
            node.Frequency += frequency;
        }

        public List<string> GetSuggestions(string prefix, int maxCount = 5)
        {
            var node = _root;
            foreach (var ch in prefix)
            {
                if (!node.Children.TryGetValue(ch, out node!))
                    return new List<string>();
            }

            var result = new List<(string word, int freq)>();
            DFS(node, prefix, result);

            return result
                .OrderByDescending(x => x.freq)
                .Take(maxCount)
                .Select(x => x.word)
                .ToList();
        }

        private void DFS(TrieNode node, string current, List<(string, int)> result)
        {
            if (node.IsWord)
                result.Add((current, node.Frequency));

            foreach (var kvp in node.Children)
                DFS(kvp.Value, current + kvp.Key, result);
        }

        public List<Word> ToWordList()
        {
            var result = new List<Word>();
            Collect(_root, "", result);
            return result;
        }

        private void Collect(TrieNode node, string current, List<Word> result)
        {
            if (node.IsWord)
                result.Add(new Word(current, node.Frequency));

            foreach (var kvp in node.Children)
                Collect(kvp.Value, current + kvp.Key, result);
        }
    }
}
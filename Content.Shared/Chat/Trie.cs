using System.Linq;

public sealed class TrieNode
{
    public Dictionary<char, TrieNode> Children { get; } = new();
    public bool IsWord { get; set; }
    public int Frequency { get; set; }
}

public sealed class Trie
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
}
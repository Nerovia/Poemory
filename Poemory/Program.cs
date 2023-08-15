using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;
using System.Drawing;
using System.Diagnostics;

while (true)
	RunSession(GetPoem());

Poem GetPoem()
{
	while (true)
	{
		Console.Write("present me thine file: ");
		var s = Console.ReadLine();
		if (s == null)
			continue;
		var path = s.Trim().Trim('"');

		if (!Path.IsPathFullyQualified(path))
		{
			Console.Error.WriteLine("ye path is not pure");
			continue;
		}

		if (!Path.Exists(path))
		{
			Console.Error.WriteLine("thine file dost not exist");
			continue;
		}

		var text = File.ReadAllText(path);
		var name = Path.GetFileNameWithoutExtension(path).ToSentenceCase();
		return new Poem(name, text);
	}
}

static void ColorWrite(string s, ConsoleColor color)
{
	var prev = Console.ForegroundColor;
	Console.ForegroundColor = color;
	Console.Write(s);
	Console.ForegroundColor = prev;
}

static void RunSession(Poem poem)
{
	var n = 0;
	var correct = 0;

	Console.WriteLine();
	Console.WriteLine($"---- {poem.Name} ----");
	Console.WriteLine();
	Console.WriteLine();

	while (n < poem.Verses.Length)
	{
		var verse = poem.Verses[n++].Replace("ß", "ss");

		int top = Console.CursorTop;
		var quiz = verse.RemoveRandomWord(out var word, out var left);
		Console.WriteLine(quiz);
		if (GetAnswer(left, top, word))
			correct++;
		Console.WriteLine();
	}

	Console.WriteLine();
	Console.Write("Ye have mastered ");
	ColorWrite(correct.ToString(), ConsoleColor.Green);
	Console.WriteLine($" / {n}, that be {correct * 100 / n}%");
	Console.WriteLine();
	Console.WriteLine();
}

static bool GetAnswer(int x, int y, string answer)
{
	var left = Console.CursorLeft;
	var top = Console.CursorTop;
	Console.SetCursorPosition(x, y);
	
	var word = "";

	while (word.Length < answer.Length)
	{
		var key = Console.ReadKey(true);
		if (char.IsLetter(key.KeyChar))
		{
			word += key.KeyChar;
			Console.Write(key.KeyChar);
		}
		else if (key.Key == ConsoleKey.Backspace)
		{
			if (word.Length > 0)
			{
				word = word.Remove(word.Length - 1);
				Console.CursorLeft -= 1;
				Console.Write('_');
				Console.CursorLeft -= 1;
			}
		}
		else if (key.Key == ConsoleKey.Enter)
		{
			break;
		}
	}

	bool correct = word.ToLower() == answer.ToLower();

	Console.SetCursorPosition(x, y);
	
	ColorWrite(answer, correct ? ConsoleColor.Green : ConsoleColor.Red);

	Console.SetCursorPosition(left, top);
	
	return correct;
}

class Poem
{
	public string Name { get; }

	public string[] Verses { get; }

	public Poem(string name, string text)
	{
		Name = name;
		Verses = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}
}

static class Extensions
{
	public static readonly Random random = new Random();

	public static T Random<T>(this IEnumerable<T> instance)
	{
		if (instance.Count() > 0)
			return instance.ElementAt(random.Next(0, instance.Count()));
		return default;
	}

	public static (int index, string text)[] SplitWithIndex(this string instance)
	{
		var words = new List<(int index, string text)>();
		(int index, string text) word = (0, "");
		for (int i = 0; i < instance.Length; i++)
		{
			if (char.IsLetter(instance[i]))
			{
				word.text += instance[i];
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(word.text))
					words.Add(word);
				word = (i + 1, "");
			}
		}
		if (word.index < instance.Length && !string.IsNullOrWhiteSpace(word.text))
			words.Add(word);
		return words.ToArray();
	}

	public static string RemoveRandomWordOld(this string instance, out string word, out int index)
	{
		var last = instance.LastIndexOf(' ');
		var n = instance.IndexOf(' ', random.Next(0, last)) + 1;
		var w = "";
		var p = new StringBuilder(instance);
		index = n;
		while (n < p.Length && char.IsLetter(instance[n]))
		{
			w += p[n];
			p[n] = '_';
			n++;
		}
		word = w;
		return p.ToString();
	}

	public static string RemoveRandomWord(this string instance, out string word, out int index)
	{
		var words = instance.SplitWithIndex();
		var wordAndIndex = words.RandomElementIndexByWeight((x) => (float)Math.Pow(x.text.Length, 2));
		var sentence = new StringBuilder(instance);
		for (int i = 0; i < wordAndIndex.text.Length; i++)
			sentence[i + wordAndIndex.index] = '_';
		index = wordAndIndex.index;
		word = wordAndIndex.text;
		return sentence.ToString();
	}


	public static T RandomElementIndexByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
	{
		float totalWeight = sequence.Sum(weightSelector);
		// The weight we are after...
		float itemWeightIndex = (float)random.NextDouble() * totalWeight;
		float currentWeightIndex = 0;
		foreach (var item in from weightedItem in sequence select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
		{
			currentWeightIndex += item.Weight;

			// If we've hit or passed the weight we are after for this item then it's the one we want....
			if (currentWeightIndex >= itemWeightIndex)
				return item.Value;
		}

		return default(T);
	}

	public static string ToSentenceCase(this string str)
	{
		return Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + m.Value[1]);
	}
}

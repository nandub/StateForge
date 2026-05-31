using System;
using System.Net.Http;

string baseUrl = ReadOption(args, "--url") ?? "http://localhost:5075";

using (HttpClient client = new HttpClient())
{
    client.BaseAddress = new Uri(baseUrl);

    HttpResponseMessage health = await client.GetAsync("/health");
    health.EnsureSuccessStatusCode();

    HttpResponseMessage set = await client.PostAsync("/session/demo/hello", null);
    set.EnsureSuccessStatusCode();

    string body = await client.GetStringAsync("/session/demo");

    if (!body.Contains("hello"))
    {
        throw new InvalidOperationException("Expected value was not returned from Kestrel harness.");
    }

    HttpResponseMessage delete = await client.DeleteAsync("/session/demo");
    delete.EnsureSuccessStatusCode();
}

Console.WriteLine("PASS: Kestrel health");
Console.WriteLine("PASS: Kestrel set");
Console.WriteLine("PASS: Kestrel get");
Console.WriteLine("PASS: Kestrel delete");

static string ReadOption(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

using System.Collections.Generic;
using FeatureHubSDK;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace FeatureHubTest
{
  /// <summary>
  /// featurehub-protocol.yaml encoding.evaluation_context.context_sha.examples - every SDK reproduces these.
  /// .NET attributes are strings, so the numeric and boolean vector is given as the strings they become.
  /// </summary>
  public class ContextShaTest
  {
    private static IEnumerable<TestCaseData> SpecVectors()
    {
      yield return new TestCaseData(new Dictionary<string, List<string>>(), "", "0");
      yield return new TestCaseData(new Dictionary<string, List<string>>
          { ["userkey"] = new List<string> { "fred" }, ["country"] = new List<string> { "nz" } },
        "country=nz,userkey=fred", "bbaf44b81bc349603f38ece814e9b741fea50e58990db3bd24988ee69eec0603");
      yield return new TestCaseData(new Dictionary<string, List<string>>
          { ["shop"] = new List<string> { "a&b", "c d" } },
        "shop=a%26b%2Cc%20d", "5f2930219932130037f572be9de4ae5cea89983dba663d9400437a63c1dea22c");
      yield return new TestCaseData(new Dictionary<string, List<string>>
          { ["my key"] = new List<string> { "x" }, ["a,b=c"] = new List<string> { "y" } },
        "a%2Cb%3Dc=y,my%20key=x", "a9d1b1707b4d38824da99d1daf06a1b9b24f7d1397d0e138153a946f1575d40a");
      yield return new TestCaseData(new Dictionary<string, List<string>>
          { ["name"] = new List<string> { "Mary Jane+O'Neil ~*()!" } },
        "name=Mary%20Jane%2BO'Neil%20~*()!", "ff5b8c6d2cc3bbebfc3285b9106b3b300082ecfc5db6d1e4e37e685d0dbbe653");
      yield return new TestCaseData(new Dictionary<string, List<string>>
          { ["city"] = new List<string> { "Zürich" }, ["greeting"] = new List<string> { "สวัสดี" } },
        "city=Z%C3%BCrich,greeting=%E0%B8%AA%E0%B8%A7%E0%B8%B1%E0%B8%AA%E0%B8%94%E0%B8%B5",
        "d0f83f10b3e4f06cc9e3d57736c75f91dce1f16ed6035f0b430ccd7f37a50dce");
      yield return new TestCaseData(new Dictionary<string, List<string>>
          { ["a!"] = new List<string> { "1" }, ["a "] = new List<string> { "2" } },
        "a!=1,a%20=2", "16cab394e891c04ba620debb77279050b6a88e4b58da3bffeb286c4428289e72");
      yield return new TestCaseData(new Dictionary<string, List<string>>
          { ["userkey"] = new List<string> { "fred" }, ["empty"] = new List<string>(), ["gone"] = new List<string> { null } },
        "userkey=fred", "6a1d1fa42d1c1917552a255a940792205cb62cc2efd6613ab5a3f75d0038518b");
      yield return new TestCaseData(new Dictionary<string, List<string>>
          { ["version"] = new List<string> { "3" }, ["beta"] = new List<string> { "true" }, ["ratio"] = new List<string> { "0.5" } },
        "beta=true,ratio=0.5,version=3", "b8ec246368e079b0f2b84682231065d867c943e964b5d5acfb557c86cb45b0fb");
    }

    [TestCaseSource(nameof(SpecVectors))]
    public void HeaderAndContextShaMatchTheSpec(Dictionary<string, List<string>> attributes, string header, string contextSha)
    {
      var built = ContextHeader.XFeatureHubHeader(attributes);

      ClassicAssert.AreEqual(header, built);
      ClassicAssert.AreEqual(contextSha, ContextHeader.ContextSha(built));
    }

    [Test]
    public void AnEmptyHeaderHasAContextShaOfZero()
    {
      ClassicAssert.AreEqual("0", ContextHeader.ContextSha(""));
      ClassicAssert.AreEqual("0", ContextHeader.ContextSha(null));
    }

    [Test]
    public void KeysAndThaiValuesAreEncodedAsEncodeUriComponentDoes()
    {
      var header = ContextHeader.XFeatureHubHeader(new Dictionary<string, List<string>>
      {
        ["userkey"] = new List<string> { "lingling kwong" },
        ["kwong pastry"] = new List<string> { "omu&co" },
        ["name"] = new List<string> { "กรณ์นภัส" },
      });

      ClassicAssert.AreEqual(
        "kwong%20pastry=omu%26co,name=%E0%B8%81%E0%B8%A3%E0%B8%93%E0%B9%8C%E0%B8%99%E0%B8%A0%E0%B8%B1%E0%B8%AA,userkey=lingling%20kwong",
        header);
    }

    [Test]
    public void SortingIsOrdinalNotCultureSensitive()
    {
      // culture-sensitive comparison puts "a-b" and "ab" (and upper/lower case) in a different order
      var header = ContextHeader.XFeatureHubHeader(new Dictionary<string, List<string>>
      {
        ["ab"] = new List<string> { "orm" },
        ["a-b"] = new List<string> { "ling" },
        ["B"] = new List<string> { "kornaphat" },
      });

      ClassicAssert.AreEqual("B=kornaphat,a-b=ling,ab=orm", header);
    }
  }
}

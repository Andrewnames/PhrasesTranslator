using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Humanizer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonTranslator
{
    class Program
    {
        static readonly string APIKEY = "2bb69405c7cf4b13996087b66df783fb";
        static string accessToken;
        private static string _cLanguageFoldersPath = "C:\\Users\\injector\\Documents\\Phrases\\";

        public static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                await TranslateRun();
            }).GetAwaiter().GetResult();
        }
        static async Task TranslateRun()
        {
            var sourceJsonFilePath = @"C:\Source\SY2-Build\SY2-I18n\Phrases\cd-XX\i18n-bundle.json";
            var sourceJson = File.ReadAllText(sourceJsonFilePath);
            JObject originalEnglishJson = JObject.Parse(sourceJson);


            var directories = Directory.GetDirectories(_cLanguageFoldersPath);
            foreach (var languageFolderWithJson in directories)
            {
                var languageName = languageFolderWithJson.Replace(_cLanguageFoldersPath, "");
                if (languageName.Length > 5)
                {
                    continue;
                }
                Console.WriteLine("Access Token is obtaining. Please wait. *************************************");
                accessToken = await GetAuthenticationToken(APIKEY);
                Console.WriteLine(languageName + " is gonna be translated ************************************* ");

                var jsonFilePath = Path.Combine(languageFolderWithJson, @"i18n-bundle.json");


                foreach (KeyValuePair<string, JToken> prop in originalEnglishJson) // about 40 seconds per language translation
                {
                    if (prop.Key == "Phrases")
                    {
                        foreach (JToken phrase in prop.Value)
                        {
                            JToken translationToken = phrase.First["Translation"];
                            if (!translationToken.HasValues)
                            {
                                string word = phrase.First.SelectToken("Source").ToString();
                                try
                                {
                                    if (DefineIfLocaleName(word) != "")
                                    {
                                        var localeAsLanguageName = DefineIfLocaleName(word);
                                        var localeName = word;

                                        string output = await Translate(localeAsLanguageName, localeName, accessToken);
                                        phrase.First["Translation"] = output.ToUpper();
                                        phrase.First["TranslationCultureCode"] = languageName;
                                        Console.WriteLine(output);

                                    }
                                    else
                                    {
                                        if (!(word.Contains('{') || word.Contains('.') || word.Contains('(') || word.Contains('/')))
                                        {
                                            word = word.Humanize(); // delete special characters
                                        }
                                        string output = await TranslateUsingGoogle(word, languageName); // pick one you like - google or microsoft

                                        string output2 = await Translate(word, languageName, accessToken);

                                        output = output.Length > output2.Length ? output : output2; //take shortest translation

                                        phrase.First["Translation"] = output;
                                        phrase.First["TranslationCultureCode"] = languageName;

                                        Console.WriteLine($" word {word} translation is : {output}");

                                    }
                                }
                                catch (Exception ex)
                                {

                                    Console.WriteLine(word + $" cant be translated because {ex.Message} ************************************* ");
                                }
                            }
                        }
                    }

                }
                string newJson = JsonConvert.SerializeObject(originalEnglishJson, Formatting.Indented);
                File.WriteAllText(jsonFilePath, newJson);
                Console.WriteLine(languageName + " is translated ************************************* ");
                Console.ReadLine();
            }
        }

        private static string DefineIfLocaleName(string word)
        {
            var result = "";
            switch (word)
            {
                case "bg-BG":
                    result = "BULGARIAN";
                    break;
                case "cs-CZ":
                    result = "CZECH";
                    break;
                case "da-DK":
                    result = "DANISH";
                    break;
                case "de-DE":
                    result = "GERMAN";
                    break;
                case "en-US":
                    result = "ENGLISH (USA)";
                    break;
                case "en-GB":
                    result = "ENGLISH (UK)";
                    break;
                case "es-ES":
                    result = "SPANISH (SPAIN)";
                    break;
                case "es-MX":
                    result = "SPANISH (MEXICO)";
                    break;
                case "fi-FI":
                    result = "FINNISH";
                    break;
                case "fr-FR":
                    result = "FRENCH";
                    break;
                case "hr-HR":
                    result = "CROATIAN";
                    break;
                case "hu-HU":
                    result = "HUNGARIAN";
                    break;
                case "is-IS":
                    result = "ICELANDIC";
                    break;
                case "it-IT":
                    result = "ITALIAN";
                    break;
                case "ja-JP":
                    result = "JAPANESE";
                    break;
                case "ko-KR":
                    result = "KOREAN";
                    break;
                case "lt-LT":
                    result = "LITHUANIAN";
                    break;
                case "lv-LV":
                    result = "LATVIAN";
                    break;
                case "nb-NO":
                    result = "LATVIAN";
                    break;
                case "nl-NL":
                    result = "DUTCH";
                    break;
                case "pt-BR":
                    result = "PORTUGUESE (BRAZIL)";
                    break;
                case "pt-PT":
                    result = "PORTUGUESE (PORTUGAL)";
                    break;
                case "ro-RO":
                    result = "ROMANIAN";
                    break;
                case "ru-RU":
                    result = "RUSSIAN";
                    break;
                case "sk-SK":
                    result = "SLOVAK";
                    break;
                case "sv-SE":
                    result = "SWEDISH";
                    break;
                case "th-TH":
                    result = "THAI";
                    break;
                case "zh-CN":
                    result = "CHINESE (SIMPLIFIED)";
                    break;
                case "zh-TW":
                    result = "CHINESE (TRADITIONAL)";
                    break;
            }
            return result;
        }

        static async Task<string> Translate(string textToTranslate, string language, string accessToken)
        {
            string url = "http://api.microsofttranslator.com/v2/Http.svc/Translate";
            string query = $"?text={System.Net.WebUtility.UrlEncode(textToTranslate)}&to={language}&contentType=text/plain";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                HttpResponseMessage response = await client.GetAsync(url + query);
                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "ERROR: " + result;

                string translatedText = XElement.Parse(result).Value;
                return translatedText;
            }
        }

        static async Task<string> TranslateUsingGoogle(string textToTranslate, string language)
        {
            var uri = HttpUtility.UrlEncode(textToTranslate);
            var url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl="
                      + "en" + "&tl=" + language + "&dt=t&q=" + uri;


            using (HttpClient client = new HttpClient())
            {

                HttpResponseMessage response = await client.GetAsync(url);
                string result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return "ERROR: " + result;

                string translatedText = result.Split('\"')[1];
                return translatedText;
            }
        }

        static async Task<string> GetAuthenticationToken(string key)
        {
            string endpoint = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken";

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);
                HttpResponseMessage response = await client.PostAsync(endpoint, null);
                string token = await response.Content.ReadAsStringAsync();
                return token;
            }
        }
    }
}

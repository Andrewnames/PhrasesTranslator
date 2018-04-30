open System.IO
open System
open Newtonsoft.Json.Linq
open FSharp.Data
open FSharp.Data.HttpRequestHeaders
open FSharp.Data.HttpContentTypes
open System.Net.Http
open System.Web
open Humanizer
open Chiron


// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.


let APIKEY = "2bb69405c7cf4b13996087b66df783fb" 
let _cLanguageFoldersPath = "C:\\Users\\injector\\Documents\\Phrases\\"
type TranslationXml = XmlProvider< """<string xmlns="http://schemas.microsoft.com/2003/10/Serialization/">¡Hola mundo!</string>""" >

let getTranslatorToken (id:string, secret:string) =           
        let client = new HttpClient()
        let endpoint = "https://api.cognitive.microsoft.com/sts/v1.0/issueToken"
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", secret)
        let response = client.PostAsync(endpoint, null)  
        let token = response.Result.Content.ReadAsStringAsync()
        token.Result

let TranslateUsingGoogle(textToTranslate:string, language) =
    let uri = HttpUtility.UrlEncode textToTranslate 
    let url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl="+"en" + "&tl=" + language + "&dt=t&q=" + uri
    let client = new HttpClient()
    let response =  client.GetAsync(url)
    let result =   response.Result.Content.ReadAsStringAsync()
    if (response.IsFaulted) then
        ()
    let array =  result.Result.Split('\"')
    array.[1]
    
let translate language text token =
    let translatedXml = 
        Http.RequestString("http://api.microsofttranslator.com/v2/Http.svc/Translate", 
                           query = [ "text", text
                                     "from", "en"
                                     "to", language ],
                           headers = [ "Authorization", "Bearer " + token ])
    TranslationXml.Parse translatedXml             
            


let DefineIfLocaleName   word =
           match word with
                    | "bg-BG"                    -> "BULGARIAN"
                    |"cs-CZ"                     -> "CZECH"
                    |"da-DK"                     -> "DANISH"
                    |"de-DE"                     -> "GERMAN"
                    | "en-US"                    -> "ENGLISH (US)"
                    | "en-GB"                    -> "ENGLISH (UK)"                   
                    | "es-ES"                    ->  "SPANISH (SPAIN)"                    
                    | "es-MX"                    ->  "SPANISH (MEXICO)"                    
                    | "fi-FI"                    ->  "FINNISH"                    
                    | "fr-FR"                    ->  "FRENCH"                    
                    | "hr-HR"                    ->  "CROATIAN"                    
                    | "hu-HU"                    ->  "HUNGARIAN"                    
                    | "is-IS"                    ->  "ICELANDIC"                    
                    | "it-IT"                    ->  "ITALIAN"                    
                    | "ja-JP"                    ->  "JAPANESE"                    
                    | "ko-KR"                    ->  "KOREAN"                    
                    | "lt-LT"                    ->  "LITHUANIAN"                    
                    | "lv-LV"                    ->  "LATVIAN"                    
                    | "nb-NO"                    ->  "LATVIAN"                    
                    | "nl-NL"                    ->  "DUTCH"                    
                    | "pt-BR"                    ->  "PORTUGUESE (BRAZIL)"                    
                    | "pt-PT"                    ->  "PORTUGUESE (PORTUGAL)"                    
                    | "ro-RO"                    ->  "ROMANIAN"                    
                    | "ru-RU"                    ->  "RUSSIAN"                    
                    | "sk-SK"                    ->  "SLOVAK"                    
                    | "sv-SE"                    ->  "SWEDISH"                    
                    | "th-TH"                    ->  "THAI"                    
                    | "zh-CN"                    ->  "CHINESE (SIMPLIFIED)"                    
                    | "zh-TW"                    ->  "CHINESE (TRADITIONAL)"
                    |_                           ->  ""         

       

let sourceJsonFilePath = @"C:\Source\SY2-Build\SY2-I18n\Phrases\cd-XX\i18n-bundle.json"
let sourceJson = File.ReadAllText(sourceJsonFilePath)
let originalEnglishJson =  JObject.Parse(sourceJson)
let directories = Directory.GetDirectories(_cLanguageFoldersPath)

 
 
type PhraseType = JsonProvider<""" { "SourceCultureCode": "cd-XX","Source": "Abort","SourceAt": "2018-01-08T16:54:09.374Z","TranslationCultureCode": "cd-XX","Translation": "Abort","TranslatedAt": "2018-01-08T16:54:09.374Z","ExampleContext": null,"UsedBy": ["ControlRoom2"],"Key": "T_Abort" } """>
let Phrase = PhraseType.Parse(""" { "SourceCultureCode": "cd-XX","Source": "Abort","SourceAt": "2018-01-08T16:54:09.374Z","TranslationCultureCode": "cd-XX","Translation": "Abort","TranslatedAt": "2018-01-08T16:54:09.374Z","ExampleContext": null,"UsedBy": ["ControlRoom2"],"Key": "T_Abort" } """)
  


let createJson json =
     Phrase.Translation = json
     Phrase
    
let TranslateStart ()=

    for languageFolderWithJson in directories do 
        let languageName = languageFolderWithJson.Replace(_cLanguageFoldersPath, "")
        if (languageName.Length > 5) then 
            ()
        else      
        printf("Access Token is obtaining. Please wait. *************************************")
        let accessToken = getTranslatorToken ("Ocp-Apim-Subscription-Key", APIKEY)
        printf "%s is gonna be translated ************************************* " languageName

        let jsonFilePath = Path.Combine(languageFolderWithJson, @"i18n-bundle.json")
        for prop in originalEnglishJson do  
            if (prop.Key.ToString().Equals "Phrases") then
                for  phrase in prop.Value do
                    let mutable translationToken = phrase.First.SelectToken("Translation")
                    let mutable TranslationCultureCode = phrase.SelectToken("TranslationCultureCode") 
                    match translationToken with
                    | null -> () 
                    | _ ->                       
                        let word = phrase.First.SelectToken("Source").ToString()
                        let isEmpty = String.IsNullOrWhiteSpace(DefineIfLocaleName word)
                        if (not  isEmpty )   then                                 
                                let localeAsLanguageName = DefineIfLocaleName(word)                                 
                                let output =  translate localeAsLanguageName  word  accessToken 
                                ()
                                
                                translationToken <- JToken.FromObject( createJson(output.ToUpper()) )
                                TranslationCultureCode <-  JToken.FromObject( languageName)
                                printfn "%s " output
 
                        else
                            if (not (word.Contains("{") || word.Contains(".") || word.Contains("(") || word.Contains("/"))) then
                                word = word.Humanize() // delete special characters                                     
                                let output =  TranslateUsingGoogle(word, languageName) // pick one you like - google or microsoft

                                let output2 =  translate languageName word accessToken
                                output = if output.Length > output2.Length then output else output2 //take shortest translation
                                translationToken <- JToken.FromObject( createJson(output) )
                                TranslationCultureCode  <-  JToken.FromObject( languageName)
                                printfn " word %s translation is : %s" word output
                               
        let newJson = Newtonsoft.Json.JsonConvert.SerializeObject originalEnglishJson   
        File.WriteAllText(jsonFilePath, newJson)
        printf "%s is translated ************************************* "   languageName                                                                                                          
        ()
 







[<EntryPoint>]
let main argv = 
    printfn "%A" argv
    TranslateStart()
    0 // return an integer exit code

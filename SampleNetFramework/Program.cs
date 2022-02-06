using IdentityModel.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;

//********************************************************************************************************************************
//**********    ICD API allows programmatic access to the International Classification of Diseases(ICD). 
//**********    More Information on ICD API and getting access to it 
//**********
//**********    https://icd.who.int/icdapi
//**********
//**********
//********************************************************************************************************************************
namespace Sample1
{
    class Program
    {
        //The _secureFile is a text file with two lines in it. The first line contains the client id and the second line client key
        static string _secureFile = @"c:\users\can\securefile.txt";

        static void Main(string[] args)
        {
            Sample1().GetAwaiter().GetResult();
        }

        static async Task Sample1()
        {
            var lines = File.ReadLines(_secureFile).ToArray();
            if (lines.Count() != 2)
            {
                Console.WriteLine("the securefile should have two lines in it. The first line contains the client id and the second line client key");
                return;
            }

            var clientId = lines[0];
            var clientSecret = lines[1];

            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync("https://icdaccessmanagement.who.int");
            if (disco.IsError) throw new Exception(disco.Error);

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = "icdapi_access",
                GrantType = "client_credentials",
                ClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            // call api
            client = new HttpClient();
            client.SetBearerToken(tokenResponse.AccessToken);

            HttpRequestMessage request;


            Console.WriteLine();
            Console.WriteLine("****************************************************************");
            Console.WriteLine("Requesting the root foundation URI...");
            request = new HttpRequestMessage(HttpMethod.Get, "https://id.who.int/icd/entity");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            request.Headers.Add("API-Version", "v2");
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }

            var resultJson = response.Content.ReadAsStringAsync().Result;
            var prettyJson = JValue.Parse(resultJson).ToString(Formatting.Indented); //convert json to a more human readable fashion
            Console.WriteLine(prettyJson);

            Console.WriteLine("Press a key to contiue");
            Console.ReadKey();//Wait until a key is pressed

            Console.WriteLine("****************************************************************");
            Console.WriteLine("Enter a search term:");
            var term = Console.ReadLine();
            request = new HttpRequestMessage(HttpMethod.Get, "https://id.who.int/icd/release/11/2021-05/mms/search?q=" + term);

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            request.Headers.Add("API-Version", "v2");
            response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }

            resultJson = response.Content.ReadAsStringAsync().Result; //Now resultJson has the resulting json string
            Console.WriteLine("****** Search result json *****");
            Console.WriteLine(resultJson);

            prettyJson = JValue.Parse(resultJson).ToString(Formatting.Indented); //convert json to a more human readable fashion
            Console.WriteLine("****** And the pretty json output *****");
            Console.WriteLine(prettyJson);

            //Now trying to parse and get titles from the search result

            Console.WriteLine("****** ICD code and titles from the search *****");
            dynamic searchResult = JsonConvert.DeserializeObject(resultJson);

            foreach (var de in searchResult.destinationEntities)
            {
                Console.WriteLine(de.theCode + " " + de.title);
            }
            Console.WriteLine("Press a key to end the program");
            Console.ReadKey(); //Wait until a key is pressed

        }
    }
}

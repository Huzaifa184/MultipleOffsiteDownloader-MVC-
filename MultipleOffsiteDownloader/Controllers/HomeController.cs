// Importing required namespaces and libraries
using Microsoft.AspNetCore.Mvc;
using MultipleOffsiteDownloader.Models;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Text;
using System.IO.Compression;

namespace MultipleOffsiteDownloader.Controllers
{
    public class HomeController : Controller
    {
        // Declaring private variables
        private readonly MyDbContext _dbcontext;
        private readonly ILogger<HomeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly List<Login> _logins;

        // Constructor with dependency injection
        public HomeController(MyDbContext context, ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _dbcontext = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _logins = new List<Login>();
        }

        // Action to display the index page
        public IActionResult Index()
        {
            var model = new List<Login>();
            try
            {
                // Retrieving login data from the database and adding it to the model
                model = _dbcontext.Logins.ToList();
            }
            catch (Exception ex)
            {
                // Logging an error if there's a problem retrieving data
                _logger.LogError(ex, "Failed to get Logins from database");
            }

            // Returning the view with the model
            return View(model);
        }

        // Action to display the privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Action to search for YouTube videos and add them to the database
        public async Task<IActionResult> Search(string keyword)
        {
            // Creating a new instance of the YouTube API client with the API key
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyBuOf62m9T1ty04pjhRUFdsoIddGB6yXac"
            });

            // Creating a search request with the specified parameters
            var searchRequest = youtubeService.Search.List("snippet");
            searchRequest.Q = keyword;
            searchRequest.Type = "video";
            searchRequest.MaxResults = 5;

            // Executing the search request asynchronously and getting the response
            var searchResponse = await searchRequest.ExecuteAsync();

            // Looping through each search result and adding it to the database
            foreach (var searchResult in searchResponse.Items)
            {
                var videoId = searchResult.Id.VideoId;

                // Creating a video request for the current video ID
                var videoRequest = youtubeService.Videos.List("snippet");
                videoRequest.Id = videoId;

                // Executing the video request asynchronously and getting the response
                var videoResponse = await videoRequest.ExecuteAsync();

                // If the video response contains at least one item, add it to the database
                if (videoResponse.Items.Count > 0)
                {
                    string email = Request.Cookies["Email"];

                    var login = new Login
                    {
                        Email = email,
                        Yturl = $"https://www.youtube.com/watch?v={videoId}",
                        VideoId = videoId,
                        Title = videoResponse.Items[0].Snippet.Title,
                        Description = videoResponse.Items[0].Snippet.Description,
                        Created = DateTime.Now
                    };
                    _dbcontext.Logins.Add(login);
                }
            }

            // Saving the changes to the database
            await _dbcontext.SaveChangesAsync();

            // Redirecting to the index page
            return RedirectToAction("Index");
        }

        

        // This block of code is an HTTP GET action method named "Login". It returns a View result that renders a login form.

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // This block of code is an HTTP POST action method named "Login". It receives a model of type "Login" from the login form, 
        // creates a new instance of "Login" with the email and creation date from the model, 
        // appends the new instance's email ID and email to the response cookies, 
        // and redirects the user to the "Index" action method.

        [HttpPost]
        public async Task<IActionResult> Login(Login model)
        {
            var login = new Login { Email = model.Email, Created = DateTime.Now };
            var loginId = login.Id;
            Response.Cookies.Append("EmailID", loginId.ToString());
            Response.Cookies.Append("Email", model.Email);
            return RedirectToAction("Index");
        }

        // This block of code is an asynchronous action method named "Download". 
        // It receives a List of strings "selectedVideos" from the client, which represent URLs of the videos to be downloaded. 
        // The method tries to execute the code within the try block. If an exception occurs, it is handled in the catch block.

        public async Task<IActionResult> Download(List<string> selectedVideos)
        {
            try
            {
                // Get the email of the user from the request cookies.
                string email = Request.Cookies["Email"];

                // Create an empty list of MemoryStreams to store the downloaded videos.
                List<MemoryStream> memoryStreams = new List<MemoryStream>();

                // Loop through each video URL in the selectedVideos list.
                foreach (var videoUrl in selectedVideos)
                {
                    // Create a new instance of Login with the email, video URL, and other properties.
                    var login = new Login
                    {
                        Email = email,
                        IsInProgress = true,
                        Timestamp = DateTime.Now,
                        Created = DateTime.Now,
                        Yturl = videoUrl
                    };

                    // Add the new instance of Login to the database context.
                    _dbcontext.Logins.Add(login);

                    // Use the HttpClientFactory to create a new HttpClient instance.
                    using (var client = _httpClientFactory.CreateClient())
                    {
                        // Send an HTTP GET request to the video URL.
                        var response = await client.GetAsync(videoUrl);

                        // Read the response content as a string.
                        var content = await response.Content.ReadAsStringAsync();

                        // Get the video title from the request URL and remove the file extension.
                        var title = response.RequestMessage.RequestUri.AbsoluteUri;
                        title = Path.GetFileNameWithoutExtension(title);

                        // Convert the content string to a MemoryStream and replace the <head> tag with a <base> tag for YouTube videos.
                        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
                        var htmlContent = Encoding.UTF8.GetString(memoryStream.ToArray());
                        htmlContent = htmlContent.Replace("<head>", "<head><base href=\"https://www.youtube.com/\" />");
                        memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(htmlContent));

                        // Set the properties of the Login instance to indicate that the video download is complete.
                        login.Title = title;
                        login.IsComplete = true;
                        login.Completed = DateTime.Now;

                        // Add the MemoryStream to the list of downloaded videos.
                        memoryStreams.Add(memoryStream);
                    }
                }

                // Save the changes to the database context.
                await _dbcontext.SaveChangesAsync();

                // Create a new MemoryStream to store the downloaded videos as a ZIP archive.
                var zipFileMemoryStream = new MemoryStream();
                using (var archive = new ZipArchive(zipFileMemoryStream, ZipArchiveMode.Create, true))
                {
                    // Loop through each MemoryStream in the list of downloaded videos.
                    int index = 1;
                    foreach (var memoryStream in memoryStreams)
                    {
                        // Create a new ZIP archive entry for each video and copy the MemoryStream data into the entry.
                        var zipEntry = archive.CreateEntry($"video{index}.html", CompressionLevel.Optimal);
                        using (var entryStream = zipEntry.Open())
                        {
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            await memoryStream.CopyToAsync(entryStream);
                        }
                        index++;
                    }
                }

                // Set the position of the MemoryStream to the beginning and return the ZIP archive as a File result.
                zipFileMemoryStream.Seek(0, SeekOrigin.Begin);
                return File(zipFileMemoryStream, "application/octet-stream", $"videos.zip");
            }
            catch (Exception ex)
            {
                // log the exception or handle it accordingly
                return StatusCode(500, "Internal server error.");
            }
        }

    }

}
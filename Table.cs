//2_q8j2LVao1x_I29nr04hZCpCB5pBrb_xHgah4cUtOdkMqvYi46Bz0w74falKDkJkcOBs6BYlNaop0RhGIk4YCHAm1l-TG-Z7VNQbTbKaKQm2oI-H41jhJXR22GcZHYx
/*using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using Newtonsoft.Json;

public class ReviewData
{
    [LoadColumn(0)]
    public string Text;

    [LoadColumn(1)]
    public int Rating;

    [ColumnName("Label"), LoadColumn(2)]
    public bool Label;
}

// Define a class to hold the prediction result
public class PredictionResult : Sent
{
    [ColumnName("PredictedLabel")]

    public bool Prediction { get; set; }
    public float Probability { get; set; }
    public float Score { get; set; }
}

public class Restaurant
{
    public string Id { get; set; }
    public string Alias { get; set; }
    public string Rating { get; set; }
}

public class Review
{
    public string Text { get; set; }
    public string Rating { get; set; }
}

public class RestaurantStream : IObservable<Restaurant>
{
    private readonly Subject<Restaurant> restaurantSubject;

    public List<Review> allReviews = new List<Review>();
    public List<Restaurant> allRestaurants = new List<Restaurant>();

    public RestaurantStream()
    {
        restaurantSubject = new Subject<Restaurant>();
    }

    public void WriteReviewsToFile(List<Review> reviews, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (Review review in reviews)
            {
                string line = $"{review.Text}\t{review.Rating}";
                writer.WriteLine(line);
            }
        }
    }

    public async Task GetReviews()
    {
        string apiKey = "2_q8j2LVao1x_I29nr04hZCpCB5pBrb_xHgah4cUtOdkMqvYi46Bz0w74falKDkJkcOBs6BYlNaop0RhGIk4YCHAm1l-TG-Z7VNQbTbKaKQm2oI-H41jhJXR22GcZHYx";
        HttpClient client = new HttpClient();

        try
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            foreach (var business in allRestaurants)
            {
                var url = $"https://api.yelp.com/v3/businesses/{business.Alias}/reviews";
                var response = await client.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                response.EnsureSuccessStatusCode();

                dynamic deserializedObject = JsonConvert.DeserializeObject<dynamic>(content);
                var reviews = deserializedObject.reviews;

                if (reviews == null)
                {
                    Console.WriteLine("No reviews found.");
                    continue;
                }
                else
                {
                    foreach (var review in reviews)
                    {
                        Console.WriteLine(review.text);
                        var newReview = new Review
                        {
                            Text = review.text,
                            Rating = review.rating.ToString()
                        };

                        allReviews.Add(newReview);
                    }
                }
            }
        }
        catch (Exception e)
        {
            restaurantSubject.OnError(e);
        }
    }

    public async Task GetRestaurants()
    {
        Console.WriteLine("GET ART FUNC");
        string apiKey = "2_q8j2LVao1x_I29nr04hZCpCB5pBrb_xHgah4cUtOdkMqvYi46Bz0w74falKDkJkcOBs6BYlNaop0RhGIk4YCHAm1l-TG-Z7VNQbTbKaKQm2oI-H41jhJXR22GcZHYx";
        HttpClient client = new HttpClient();

        var url = "https://api.yelp.com/v3/businesses/search?sort_by=best_match&limit=20&location=NYC";

        try
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            dynamic deserializedObject = JsonConvert.DeserializeObject<dynamic>(content);
            var businesses = deserializedObject.businesses;

            if (businesses == null)
            {
                Console.WriteLine("No businesses found.");
                return;
            }

            foreach (var business in businesses)
            {
                Console.WriteLine("Business found.");
                var newArticle = new Restaurant
                {
                    Id = business.id,
                    Alias = business.alias,
                    Rating = business.rating
                };
                allRestaurants.Add(newArticle);
                restaurantSubject.OnNext(newArticle);
            }

            // Signal that all articles have been retrieved
            restaurantSubject.OnCompleted();
        }
        catch (Exception e)
        {
            restaurantSubject.OnError(e);
        }
    }

    public IDisposable Subscribe(IObserver<Restaurant> observer)
    {
        return restaurantSubject.Subscribe(observer);
    }
}

public class RestaurantObserver : IObserver<Restaurant>
{
    private readonly string name;

    public RestaurantObserver(string name)
    {
        this.name = name;
    }

    public void OnNext(Restaurant article)
    {
        Console.WriteLine($"{name}: {article.Id}!");
    }

    public void OnError(Exception e)
    {
        Console.WriteLine($"{name}: An error occurred: {e.Message}");
    }

    public void OnCompleted()
    {
        Console.WriteLine($"{name}: All articles successfully returned.");
    }
}

public class Program
{
    public async static Task Main()
    {
        // Create an article stream
        var articleStream = new RestaurantStream();

        // Add several observers
        var observer1 = new RestaurantObserver("Observer 1");
        var observer2 = new RestaurantObserver("Observer 2");
        var observer3 = new RestaurantObserver("Observer 3");

        // Subscribe the observers to the filtered stream
        var subscription1 = articleStream.Subscribe(observer1);
        var subscription2 = articleStream.Subscribe(observer2);
        var subscription3 = articleStream.Subscribe(observer3);

        // Retrieve articles
        await articleStream.GetRestaurants();
        Console.WriteLine(articleStream.allRestaurants.Count);

        // Retrieve reviews
        await articleStream.GetReviews();
        string filePath = "reviews.txt";
        articleStream.WriteReviewsToFile(articleStream.allReviews, filePath);

        var mlContext = new MLContext();

        // Load the data from the text file
        var dataPath = "reviews.txt";
        var data = mlContext.Data.LoadFromTextFile<ReviewData>(dataPath, separatorChar: '\t', hasHeader: true);

        // Define the pipeline

        var pipeline = mlContext.Transforms.Expression("Label", "(x)=> x>=3?true:false", "Rating")
            .Append(mlContext.Transforms.Text.FeaturizeText("Features", nameof(ReviewData.Text)))
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());



        // Split the data into training and testing sets
        var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

        // Train the binary classification model
        var model = pipeline.Fit(split.TrainSet);


        // Make predictions on the test data
        var transformedTestData = model.Transform(split.TestSet);
        var predictions = mlContext.Data.CreateEnumerable<PredictionResult>(transformedTestData, reuseRowObject: false);

        // Display the predicted labels and probabilities
        Console.WriteLine("Predictions:");
        foreach (var prediction in predictions)
        {
            Console.WriteLine($"Predicted Label: {prediction.Prediction}");
            Console.WriteLine($"Probability: {prediction.Probability}");
            Console.WriteLine();
        }

        // Evaluate the model
        var metrics = mlContext.BinaryClassification.Evaluate(transformedTestData);

        // Display the evaluation metrics
        Console.WriteLine("Evaluation Metrics:");
        Console.WriteLine($"  Accuracy: {metrics.Accuracy:P2}");
        Console.WriteLine($"  AUC: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"  F1 Score: {metrics.F1Score:P2}");
        Console.WriteLine($"  Log-Loss: {metrics.LogLoss:F4}");

        // Initialize counters for positive and negative comments
        int positiveCount = 0;
        int negativeCount = 0;

        // Count the positive and negative comments
        foreach (var prediction in predictions)
        {
            if (prediction.Prediction)
            {
                positiveCount++;
            }
            else
            {
                negativeCount++;
            }
        }

        // Calculate the percentages
        int totalCount = positiveCount + negativeCount;
        double positivePercentage = (double)positiveCount / totalCount * 100;
        double negativePercentage = (double)negativeCount / totalCount * 100;

        // Display the comment sentiment percentages
        Console.WriteLine("Comment Sentiment Percentages:");
        Console.WriteLine($"Positive: {positivePercentage}%");
        Console.WriteLine($"Negative: {negativePercentage}%");

        // Dispose the subscriptions
        subscription1.Dispose();
        subscription2.Dispose();
        subscription3.Dispose();
    }
}*/
namespace Yelp_restorani
{
    internal class Table
    {
        public Table()
        {
        }
    }
}
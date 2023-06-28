//2_q8j2LVao1x_I29nr04hZCpCB5pBrb_xHgah4cUtOdkMqvYi46Bz0w74falKDkJkcOBs6BYlNaop0RhGIk4YCHAm1l-TG-Z7VNQbTbKaKQm2oI-H41jhJXR22GcZHYx
using Microsoft.ML;
using Microsoft.ML.Data;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Reactive.Subjects;

public class ReviewData
{
    [LoadColumn(0)]
    public string Text;

    [LoadColumn(1)]
    public int Rating;

    [ColumnName("Label"), LoadColumn(2)]
    public bool Label;
}
public class PredictionResult
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

    public async Task GetRestaurants(string location)
    {
        Console.WriteLine("....GETTING COMMENTS....");
        string apiKey = "2_q8j2LVao1x_I29nr04hZCpCB5pBrb_xHgah4cUtOdkMqvYi46Bz0w74falKDkJkcOBs6BYlNaop0RhGIk4YCHAm1l-TG-Z7VNQbTbKaKQm2oI-H41jhJXR22GcZHYx";
        HttpClient client = new HttpClient();

        var url = $"https://api.yelp.com/v3/businesses/search?location={location}&categories=restaurants";

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
                var newRestaurant = new Restaurant
                {
                    Id = business.id,
                    Alias = business.alias,
                    Rating = business.rating
                };
                allRestaurants.Add(newRestaurant);
                restaurantSubject.OnNext(newRestaurant);
            }
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
    //private int positiveCount;
    //private int negativeCount;

    public RestaurantObserver(string name)
    {
        this.name = name;
        //positiveCount = 0;
        //negativeCount = 0;
    }

    public void OnNext(Restaurant restaurant)
    {
        Console.WriteLine($"{name}: {restaurant.Id}!");
    }

    public void OnError(Exception e)
    {
        Console.WriteLine($"{name}: An error occurred: {e.Message}");
    }

    public void OnCompleted()
    {
        Console.WriteLine($"{name}: All restaurants successfully returned.");
    }
    /*public void AddReviewSentiment(bool isPositive)
    {
        if (isPositive)
        {
            positiveCount++;
        }
        else
        {
            negativeCount++;
        }
    }

    private void PrintReviewStatistics()
    {
        int totalCount = positiveCount + negativeCount;
        double positivePercentage = (double)positiveCount / totalCount * 100;
        double negativePercentage = (double)negativeCount / totalCount * 100;

        Console.WriteLine($"Positive reviews: {positiveCount} ({positivePercentage:F2}%)");
        Console.WriteLine($"Negative reviews: {negativeCount} ({negativePercentage:F2}%)");
    }*/
}

public class Program
{
    public async static Task Main()
    {
        var restaurantStream = new RestaurantStream();

        var observer1 = new RestaurantObserver("Observer 1");
        var observer2 = new RestaurantObserver("Observer 2");
        var observer3 = new RestaurantObserver("Observer 3");

        var subscription1 = restaurantStream.Subscribe(observer1);
        var subscription2 = restaurantStream.Subscribe(observer2);
        var subscription3 = restaurantStream.Subscribe(observer3);

        string location;
        Console.WriteLine("Enter location:");
        location = System.Console.ReadLine()!;
        await restaurantStream.GetRestaurants(location);

        Console.WriteLine("We have: " + restaurantStream.allRestaurants.Count + " restaurants\n");


        await restaurantStream.GetReviews();
        string filePath = "reviews.txt";
        restaurantStream.WriteReviewsToFile(restaurantStream.allReviews, filePath);

        var mlContext = new MLContext();

        var dataPath = "reviews.txt";
        var data = mlContext.Data.LoadFromTextFile<ReviewData>(dataPath, separatorChar: '\t', hasHeader: true);

        var pipeline = mlContext.Transforms.Expression("Label", "(x) => x >= 3 ? true : false", "Rating")
            .Append(mlContext.Transforms.Text.FeaturizeText("Features", nameof(ReviewData.Text))) // Change this line
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());




        var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2);

        var model = pipeline.Fit(split.TrainSet);

        var transformedTestData = model.Transform(split.TestSet);
        var predictions = mlContext.Data.CreateEnumerable<PredictionResult>(transformedTestData, reuseRowObject: false);

        Console.WriteLine("Predictions:");
        foreach (var prediction in predictions)
        {
            Console.WriteLine($"Predicted Label: {prediction.Prediction}");
            Console.WriteLine($"Probability: {prediction.Probability}");
            Console.WriteLine();
        }

        var metrics = mlContext.BinaryClassification.Evaluate(transformedTestData);

        Console.WriteLine("Evaluation Metrics:");
        Console.WriteLine($"  Accuracy: {metrics.Accuracy:P2}");
        Console.WriteLine($"  AUC: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"  F1 Score: {metrics.F1Score:P2}");
        Console.WriteLine($"  Log-Loss: {metrics.LogLoss:F4}");

        int positiveCount = 0;
        int negativeCount = 0;

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

        int totalCount = positiveCount + negativeCount;
        double positivePercentage = (double)positiveCount / totalCount * 100;
        double negativePercentage = (double)negativeCount / totalCount * 100;

        Console.WriteLine("Comment Sentiment Percentages:");
        Console.WriteLine($"Positive: {positivePercentage}%");
        Console.WriteLine($"Negative: {negativePercentage}%");

        subscription1.Dispose();
        subscription2.Dispose();
        subscription3.Dispose();
    }
}

using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

public class RatingController : Controller
{
    private readonly DB db;
    private readonly IWebHostEnvironment en;
    private readonly Helper hp;

    public RatingController(DB db, IWebHostEnvironment en, Helper hp)
    {

        this.db = db;
        this.en = en;
        this.hp = hp;
    }
    public bool CheckRating(string comment, int rating)
    {
        if (rating <= 2 && string.IsNullOrEmpty(comment))
        {
            return false;
        }
        return true;
    }

    [HttpPost]
    public IActionResult AddReview(ReviewVM vm)
    {
        // Validate the input
        if (!CheckRating(vm.comment, vm.rating))
        {
            ModelState.AddModelError("comment", "Please enter a comment when the rating is 1 or 2.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // Check if the member has already reviewed this month
        var currentMonthReview = db.Reviews
            .FirstOrDefault(r => r.MemberId == User.Identity.Name &&
                                 r.CommentDate.Month == DateTime.Now.Month &&
                                 r.CommentDate.Year == DateTime.Now.Year);

        if (currentMonthReview != null)
        {
            ModelState.AddModelError(string.Empty, "You can only leave one review per month.");
            return View(vm);
        }

        // Generate a new Review ID
        string newId;
        var maxId = db.Reviews
                      .Where(r => r.Id.StartsWith("Review"))
                      .OrderByDescending(r => r.Id)
                      .FirstOrDefault()?.Id;

        if (maxId == null)
        {
            newId = "Review01";
        }
        else
        {
            int numericPart = int.Parse(maxId.Substring(6));
            newId = $"Review{(numericPart + 1):D2}";
        }

        // Create a new review
        var newReview = new Review
        {
            Id = newId,
            Rating = vm.rating,
            Comment = vm.comment,
            CommentDate = DateOnly.FromDateTime(DateTime.Now), // Current date
            numberOfComments = currentMonthReview?.numberOfComments + 1 ?? 1,
            MemberId = User.Identity.Name
        };

        var member=db.Members.Find(User.Identity.Name);
        member.Points += 50;
        // Save to database
        db.Reviews.Add(newReview);
        db.SaveChanges();

        TempData["Info"] = "Your review has been added successfully!";
        return RedirectToAction("Index","Home");
    }

}

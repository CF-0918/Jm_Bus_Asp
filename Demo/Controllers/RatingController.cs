using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = "Member")]
    public IActionResult AddReview(ReviewVM vm)
    {
        // Check if rating requires a comment
        if ((vm.rating == 1 || vm.rating == 2) && string.IsNullOrWhiteSpace(vm.comment))
        {
            TempData["Error"] = "Please enter a comment when the rating is 1 or 2.";
            return RedirectToAction("Index", "Home");
        }

        // Check if the member has already reviewed this month
        var currentMonthReview = db.Reviews
            .FirstOrDefault(r => r.MemberId == User.Identity.Name &&
                                 r.CommentDate.Month == DateTime.Now.Month &&
                                 r.CommentDate.Year == DateTime.Now.Year);

        if (currentMonthReview != null)
        {
            TempData["Info"] = "You can only leave one review per month.";
            return RedirectToAction("Index", "Home");
        }

        // Generate a new Review ID
        string newId;
        var maxId = db.Reviews
            .Where(r => r.Id.StartsWith("Review"))
            .OrderByDescending(r => r.Id)
            .FirstOrDefault()?.Id;

        newId = maxId == null
            ? "Review01"
            : $"Review{(int.Parse(maxId.Substring(6)) + 1):D2}";

        // Create a new review object
        var newReview = new Review
        {
            Id = newId,
            Rating = vm.rating,
            Comment = vm.comment,
            CommentDate = DateOnly.FromDateTime(DateTime.Now), // Current date
            numberOfComments = 1, // Assuming this is for the current review cycle
            MemberId = User.Identity.Name
        };

        // Update member's points
        var member = db.Members.Find(User.Identity.Name);
        if (member != null)
        {
            member.Points += 50;
        }

        // Save the review to the database
        db.Reviews.Add(newReview);
        db.SaveChanges();

        // Set success message
        TempData["Info"] = "Your review has been added successfully!";
        return RedirectToAction("Index", "Home");
    }


}

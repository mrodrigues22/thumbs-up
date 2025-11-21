using ThumbsUpApi.DTOs;
using ThumbsUpApi.Models;

namespace ThumbsUpApi.Mappers;

public class ReviewMapper
{
    public ReviewResponse ToResponse(Review review)
    {
        return new ReviewResponse
        {
            Id = review.Id,
            Status = review.Status,
            Comment = review.Comment,
            ReviewedAt = review.ReviewedAt
        };
    }
}

using BuggyApp.Models;

namespace BuggyApp.Data;

public sealed class MockedDb
{
    private const int InitialCourseCount = 300;
    private const int InitialBasketCount = 10;
    private const int InitialRegistrationCount = 100;

    private readonly Lock _sync = new();
    private readonly Dictionary<int, Course> _courses = [];
    private readonly HashSet<int> _basketCourseIds = [];
    private readonly List<Registration> _registrations = [];

    public MockedDb()
    {
        Reset();
    }

    public DashboardSummary GetDashboardSummary()
    {
        lock (_sync)
        {
            return new DashboardSummary(
                _courses.Count,
                _basketCourseIds.Count,
                _registrations.Count);
        }
    }

    public PagedResult<Course> GetCourses(string? category, int page, int pageSize)
    {
        ValidatePaging(page, pageSize);

        lock (_sync)
        {
            IEnumerable<Course> query = _courses.Values.OrderBy(course => course.Id);

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(course =>
                    course.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            var filtered = query.ToArray();
            var items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return new PagedResult<Course>(items, page, pageSize, filtered.Length);
        }
    }

    public Course? GetCourse(int courseId)
    {
        lock (_sync)
        {
            return _courses.GetValueOrDefault(courseId);
        }
    }

    public bool IsInBasket(int courseId)
    {
        lock (_sync)
        {
            return _basketCourseIds.Contains(courseId);
        }
    }

    public bool AddToBasket(int courseId)
    {
        lock (_sync)
        {
            EnsureCourseExists(courseId);
            return _basketCourseIds.Add(courseId);
        }
    }

    public bool RemoveFromBasket(int courseId)
    {
        lock (_sync)
        {
            return _basketCourseIds.Remove(courseId);
        }
    }

    public IReadOnlyList<Course> GetBasket()
    {
        lock (_sync)
        {
            return _basketCourseIds
                .Order()
                .Select(courseId => _courses[courseId])
                .ToArray();
        }
    }

    public void ClearBasket()
    {
        lock (_sync)
        {
            _basketCourseIds.Clear();
        }
    }

    public Registration RegisterBasket()
    {
        lock (_sync)
        {
            if (_basketCourseIds.Count == 0)
            {
                throw new InvalidOperationException("Cannot register an empty basket.");
            }

            var registration = new Registration(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                _basketCourseIds
                    .Order()
                    .Select(courseId => _courses[courseId])
                    .ToArray());

            _registrations.Insert(0, registration);
            _basketCourseIds.Clear();
            return registration;
        }
    }

    public PagedResult<Registration> GetRegistrations(int page, int pageSize)
    {
        ValidatePaging(page, pageSize);

        lock (_sync)
        {
            var items = _registrations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToArray();

            return new PagedResult<Registration>(items, page, pageSize, _registrations.Count);
        }
    }

    public Registration? GetRegistration(Guid registrationId)
    {
        lock (_sync)
        {
            return _registrations.FirstOrDefault(item => item.Id == registrationId);
        }
    }

    public bool DeleteRegistration(Guid registrationId)
    {
        lock (_sync)
        {
            var index = _registrations.FindIndex(item => item.Id == registrationId);
            if (index < 0)
            {
                return false;
            }

            _registrations.RemoveAt(index);
            return true;
        }
    }

    public void ClearHistory()
    {
        lock (_sync)
        {
            _registrations.Clear();
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _courses.Clear();
            _basketCourseIds.Clear();
            _registrations.Clear();

            foreach (var course in CreateCourses())
            {
                _courses.Add(course.Id, course);
            }

            foreach (var courseId in Enumerable.Range(1, InitialBasketCount))
            {
                _basketCourseIds.Add(courseId);
            }

            var courses = _courses.Values.OrderBy(course => course.Id).ToArray();
            for (var index = 0; index < InitialRegistrationCount; index++)
            {
                var registrationCourses = Enumerable.Range(0, 3 + index % 6)
                    .Select(offset => courses[(index * 7 + offset) % courses.Length])
                    .ToArray();

                _registrations.Add(new Registration(
                    CreateDeterministicGuid(index),
                    DateTimeOffset.UtcNow.AddDays(-index),
                    registrationCourses));
            }
        }
    }

    private static IEnumerable<Course> CreateCourses()
    {
        string[] categories =
        [
            "Computing",
            "Business",
            "Engineering",
            "Science",
            "Arts",
            "Humanities"
        ];

        for (var id = 1; id <= InitialCourseCount; id++)
        {
            var category = categories[(id - 1) % categories.Length];
            yield return new Course(
                id,
                $"{category[..3].ToUpperInvariant()}{100 + id}",
                $"{category} Course {id}",
                category,
                $"Instructor {(id - 1) % 24 + 1}",
                $"A locally generated {category.ToLowerInvariant()} course used for GUI stress testing.");
        }
    }

    private static Guid CreateDeterministicGuid(int value)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, value + 1);
        return new Guid(bytes);
    }

    private void EnsureCourseExists(int courseId)
    {
        if (!_courses.ContainsKey(courseId))
        {
            throw new KeyNotFoundException($"Course {courseId} does not exist.");
        }
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }
    }
}

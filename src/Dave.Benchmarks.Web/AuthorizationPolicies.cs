namespace Dave.Benchmarks.Web;

public static class AuthorizationPolicies
{
    public const string GitLabCi = nameof(GitLabCi);
    public const string GitLabProtectedRef = nameof(GitLabProtectedRef);
    public const string ObservationCurator = nameof(ObservationCurator);
    public const string DevelopmentOrGitLabProtectedRef = nameof(DevelopmentOrGitLabProtectedRef);
}

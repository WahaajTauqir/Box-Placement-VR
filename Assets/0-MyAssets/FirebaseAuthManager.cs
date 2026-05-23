using System;
using System.Collections;
using Firebase;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Email/password authentication controller for Firebase Auth.
/// Add this to an active manager object and wire the TMP input fields and UnityEvents.
/// </summary>
[DisallowMultipleComponent]
public class FirebaseAuthManager : MonoBehaviour
{
    [Serializable]
    public class StringEvent : UnityEvent<string>
    {
    }

    [Header("Login Inputs")]
    [SerializeField] TMP_InputField loginEmailInput;
    [SerializeField] TMP_InputField loginPasswordInput;

    [Header("Sign Up Inputs")]
    [SerializeField] TMP_InputField signupEmailInput;
    [SerializeField] TMP_InputField signupPasswordInput;
    [SerializeField] TMP_InputField signupConfirmPasswordInput;

    [Header("Feedback")]
    [SerializeField] TMP_Text statusText;
    [SerializeField] bool clearPasswordsAfterSuccess = true;
    [SerializeField] bool initializeOnAwake = true;
    [SerializeField] bool checkSavedLoginOnInitialize = true;

    [Header("Events")]
    [SerializeField] UnityEvent onFirebaseReady;
    [SerializeField] UnityEvent onAutoLoginSuccess;
    [SerializeField] UnityEvent onAutoLoginUnavailable;
    [SerializeField] UnityEvent onLoginSuccess;
    [SerializeField] UnityEvent onSignupSuccess;
    [SerializeField] UnityEvent onLogout;
    [SerializeField] StringEvent onLoginFailed;
    [SerializeField] StringEvent onSignupFailed;
    [SerializeField] StringEvent onInitializationFailed;
    [SerializeField] StringEvent onAuthenticatedUserId;

    FirebaseAuth auth;
    bool isInitializing;
    bool isBusy;
    bool isFirebaseReady;

    public bool IsBusy => isBusy || isInitializing;
    public bool IsFirebaseReady => isFirebaseReady;
    public FirebaseUser CurrentUser => auth?.CurrentUser;

    void Awake()
    {
        if (initializeOnAwake)
            InitializeFirebase();
    }

    /// <summary>
    /// Initializes Firebase and verifies native dependencies. Safe to call more than once.
    /// </summary>
    public void InitializeFirebase()
    {
        if (isFirebaseReady)
        {
            if (checkSavedLoginOnInitialize)
                CheckSavedLogin();
            return;
        }

        if (isInitializing)
            return;

        StartCoroutine(InitializeFirebaseRoutine());
    }

    /// <summary>
    /// Checks whether Firebase restored a previously authenticated user session.
    /// Firebase restores its auth session; it does not expose saved passwords.
    /// </summary>
    public void CheckSavedLogin()
    {
        if (!isFirebaseReady || auth == null)
        {
            SetStatus("Firebase is not ready.");
            return;
        }

        FirebaseUser user = auth.CurrentUser;
        if (user == null)
        {
            SetStatus("No saved login found.");
            onAutoLoginUnavailable?.Invoke();
            return;
        }

        SetStatus("Signed in from saved session.");
        onAuthenticatedUserId?.Invoke(user.UserId);
        onAutoLoginSuccess?.Invoke();
    }

    /// <summary>
    /// Attempts email/password login using the login input fields.
    /// </summary>
    public void Login()
    {
        string email = GetEmail(loginEmailInput);
        string password = GetPassword(loginPasswordInput);

        if (!ValidateCredentials(email, password, false, out string error))
        {
            ReportLoginFailure(error);
            return;
        }

        if (!CanStartRequest("login"))
            return;

        StartCoroutine(LoginRoutine(email, password));
    }

    /// <summary>
    /// Attempts account creation using the sign-up input fields.
    /// </summary>
    public void Register()
    {
        string email = GetEmail(signupEmailInput);
        string password = GetPassword(signupPasswordInput);
        string confirmation = GetPassword(signupConfirmPasswordInput);

        if (!ValidateCredentials(email, password, true, out string error))
        {
            ReportSignupFailure(error);
            return;
        }

        if (signupConfirmPasswordInput != null && password != confirmation)
        {
            ReportSignupFailure("Passwords do not match.");
            return;
        }

        if (!CanStartRequest("sign up"))
            return;

        StartCoroutine(RegisterRoutine(email, password));
    }

    /// <summary>
    /// Signs out the currently authenticated user.
    /// </summary>
    public void Logout()
    {
        if (!isFirebaseReady || auth == null)
        {
            SetStatus("Firebase is not ready.");
            return;
        }

        auth.SignOut();
        SetStatus("Signed out.");
        onLogout?.Invoke();
    }

    IEnumerator InitializeFirebaseRoutine()
    {
        isInitializing = true;
        SetStatus("Connecting to Firebase...");

        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        isInitializing = false;
        if (dependencyTask.IsCanceled)
        {
            ReportInitializationFailure("Firebase initialization was cancelled.");
            yield break;
        }

        if (dependencyTask.IsFaulted)
        {
            ReportInitializationFailure(
                "Firebase could not initialize: " + GetExceptionMessage(dependencyTask.Exception));
            yield break;
        }

        if (dependencyTask.Result != DependencyStatus.Available)
        {
            ReportInitializationFailure(
                "Firebase dependencies are unavailable: " + dependencyTask.Result + ".");
            yield break;
        }

        try
        {
            auth = FirebaseAuth.DefaultInstance;
            isFirebaseReady = auth != null;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            ReportInitializationFailure("Firebase Auth could not initialize.");
            yield break;
        }

        if (!isFirebaseReady)
        {
            ReportInitializationFailure("Firebase Auth is unavailable.");
            yield break;
        }

        SetStatus("Ready to sign in.");
        onFirebaseReady?.Invoke();

        if (checkSavedLoginOnInitialize)
            CheckSavedLogin();
    }

    IEnumerator LoginRoutine(string email, string password)
    {
        isBusy = true;
        SetStatus("Signing in...");

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        isBusy = false;
        if (loginTask.IsCanceled)
        {
            ReportLoginFailure("Login was cancelled.");
            yield break;
        }

        if (loginTask.IsFaulted)
        {
            ReportLoginFailure(GetAuthErrorMessage(loginTask.Exception, false));
            yield break;
        }

        FirebaseUser user = loginTask.Result.User;
        if (user == null)
        {
            ReportLoginFailure("Firebase returned no authenticated user.");
            yield break;
        }

        if (clearPasswordsAfterSuccess && loginPasswordInput != null)
            loginPasswordInput.text = "";

        SetStatus("Login successful.");
        onAuthenticatedUserId?.Invoke(user.UserId);
        onLoginSuccess?.Invoke();
    }

    IEnumerator RegisterRoutine(string email, string password)
    {
        isBusy = true;
        SetStatus("Creating account...");

        var signupTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => signupTask.IsCompleted);

        isBusy = false;
        if (signupTask.IsCanceled)
        {
            ReportSignupFailure("Sign up was cancelled.");
            yield break;
        }

        if (signupTask.IsFaulted)
        {
            ReportSignupFailure(GetAuthErrorMessage(signupTask.Exception, true));
            yield break;
        }

        FirebaseUser user = signupTask.Result.User;
        if (user == null)
        {
            ReportSignupFailure("Firebase returned no new user.");
            yield break;
        }

        if (clearPasswordsAfterSuccess)
        {
            if (signupPasswordInput != null)
                signupPasswordInput.text = "";
            if (signupConfirmPasswordInput != null)
                signupConfirmPasswordInput.text = "";
        }

        SetStatus("Account created successfully.");
        onAuthenticatedUserId?.Invoke(user.UserId);
        onSignupSuccess?.Invoke();
    }

    bool CanStartRequest(string action)
    {
        if (IsBusy)
        {
            SetStatus("Please wait for the current request to finish.");
            return false;
        }

        if (!isFirebaseReady || auth == null)
        {
            SetStatus($"Firebase is not ready for {action}. Initializing...");
            InitializeFirebase();
            return false;
        }

        return true;
    }

    static bool ValidateCredentials(string email, string password, bool signingUp, out string error)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            error = "Enter an email address.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            error = "Enter a password.";
            return false;
        }

        if (signingUp && password.Length < 6)
        {
            error = "Password must be at least 6 characters.";
            return false;
        }

        error = null;
        return true;
    }

    static string GetEmail(TMP_InputField field)
    {
        return field == null ? "" : field.text.Trim();
    }

    static string GetPassword(TMP_InputField field)
    {
        return field == null ? "" : field.text;
    }

    void ReportLoginFailure(string message)
    {
        SetStatus(message);
        onLoginFailed?.Invoke(message);
    }

    void ReportSignupFailure(string message)
    {
        SetStatus(message);
        onSignupFailed?.Invoke(message);
    }

    void ReportInitializationFailure(string message)
    {
        Debug.LogError(message, this);
        SetStatus(message);
        onInitializationFailed?.Invoke(message);
    }

    void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    static string GetAuthErrorMessage(Exception exception, bool signingUp)
    {
        Exception baseException = GetBaseException(exception);
        if (!(baseException is FirebaseException firebaseException))
            return signingUp ? "Sign up failed. Please try again." : "Login failed. Please try again.";

        string errorCode = ((AuthError)firebaseException.ErrorCode).ToString();
        switch (errorCode)
        {
            case "InvalidEmail":
                return "Enter a valid email address.";
            case "MissingEmail":
                return "Enter an email address.";
            case "MissingPassword":
                return "Enter a password.";
            case "WeakPassword":
                return "The password is too weak.";
            case "EmailAlreadyInUse":
                return "An account already exists for this email.";
            case "WrongPassword":
            case "UserNotFound":
            case "InvalidCredential":
                return "Email or password is incorrect.";
            case "UserDisabled":
                return "This account has been disabled.";
            case "OperationNotAllowed":
                return "Email/password sign-in is not enabled in Firebase.";
            case "NetworkRequestFailed":
                return "Network error. Check your internet connection.";
            case "TooManyRequests":
                return "Too many attempts. Please try again later.";
            default:
                return signingUp ? "Sign up failed. Please try again." : "Login failed. Please try again.";
        }
    }

    static string GetExceptionMessage(Exception exception)
    {
        Exception baseException = GetBaseException(exception);
        return string.IsNullOrWhiteSpace(baseException?.Message)
            ? "Unknown error."
            : baseException.Message;
    }

    static Exception GetBaseException(Exception exception)
    {
        if (exception is AggregateException aggregateException)
        {
            AggregateException flattened = aggregateException.Flatten();
            if (flattened.InnerExceptions.Count > 0)
                return flattened.InnerExceptions[0];
        }

        return exception?.GetBaseException();
    }
}

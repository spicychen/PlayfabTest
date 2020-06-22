using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;
using System;
using Facebook.Unity;
using LoginResult = PlayFab.ClientModels.LoginResult;


public enum Authtypes
{
    None,
    Silent,
    EmailAndPassword,
    RegisterPlayfabAccount,
    Steam,
    FaceBook
}

public class PlayfabLoginManager : MonoBehaviour
{
    public InputField UsernameUI;
    public InputField PasswordUI;
    public InputField ConfirmPasswordUI;
    public Toggle RememberMeUI;

    public bool ClearPlayerPrefs;
    public bool ForceLink = false;
    public string Username;

    public Button LoginButton;
    public Button PlayAsGuestButton;
    public Button RegisterButton;
    public Button LoginWithFaceBookButton;
    public Button CancelRegisterButton;
    public Button LogoutButton;
    public Button ContinueButton;
    
    public GameObject RegisterPanel;
    public GameObject SigninPanel;
    public GameObject Panel;
    public GameObject SigninSuccessPanel;
    public GameObject PlayfabStatsController;
    public Text StatusText;
    public Text SigninSuccessText;

    public GetPlayerCombinedInfoRequestParams InfoRequestParams;

    // Accessbility for PlayFab ID & Session Tickets
    public static string PlayFabId { get { return _playFabId; } }
    private static string _playFabId;

    public static string SessionTicket { get { return _sessionTicket; } }
    private static string _sessionTicket;

    private const string _LoginRememberKey = "PlayFabLoginRemember";
    private const string _PlayFabRememberMeIdKey = "PlayFabIdPassGuid";
    private const string _PlayFabAuthTypeKey = "PlayFabAuthType";

    public bool RememberMe
    {
        get
        {
            return PlayerPrefs.GetInt(_LoginRememberKey, 0) == 0 ? false : true;
        }
        set
        {
            PlayerPrefs.SetInt(_LoginRememberKey, value ? 1 : 0);
        }
    }

    private string RememberMeId
    {
        get
        {
            return PlayerPrefs.GetString(_PlayFabRememberMeIdKey, "");
        }
        set
        {
            var guid = value ?? Guid.NewGuid().ToString();
            PlayerPrefs.SetString(_PlayFabRememberMeIdKey, guid);
        }
    }

    public Authtypes AuthType
    {
        get
        {
            return (Authtypes)PlayerPrefs.GetInt(_PlayFabAuthTypeKey, 0);
        }
        set
        {
            PlayerPrefs.SetInt(_PlayFabAuthTypeKey, (int)value);
        }
    }

    public void Awake()
    {
        if (ClearPlayerPrefs)
        {
            UnlinkSilentAuth();
            ClearRememberMe();
            AuthType = Authtypes.None;
        }

        RememberMeUI.isOn = RememberMe;
        RememberMeUI.onValueChanged.AddListener((toggle) => { RememberMe = toggle; });
    }

    // Start is called before the first frame update
    void Start()
    {
        Panel.SetActive(false);
        RegisterPanel.SetActive(false);
        SigninPanel.SetActive(true);

        LoginButton.onClick.AddListener(OnLoginClicked);
        PlayAsGuestButton.onClick.AddListener(OnPlayAsGuestClicked);
        RegisterButton.onClick.AddListener(OnRegisterButtonClicked);
        CancelRegisterButton.onClick.AddListener(OnCancelRegisterButtonClicked);
        LogoutButton.onClick.AddListener(OnLogoutClicked);
        LoginWithFaceBookButton.onClick.AddListener(OnLoginWithFaceBookClicked);
        ContinueButton.onClick.AddListener(OnContinueButtonClicked);

        Authenticate();
    }

    public void UnlinkSilentAuth()
    {
        SilentlyAuthenticate((result) =>
        {
            PlayFabClientAPI.UnlinkCustomID(new UnlinkCustomIDRequest()
            {
                CustomId = SystemInfo.deviceUniqueIdentifier
            }, null, null);

        });
    }

    public void ClearRememberMe()
    {
        PlayerPrefs.DeleteKey(_LoginRememberKey);
        PlayerPrefs.DeleteKey(_PlayFabRememberMeIdKey);
        PlayerPrefs.DeleteKey(_PlayFabAuthTypeKey);
    }

    private void OnLoginClicked()
    {
        StatusText.text = string.Format("Logging In As {0} ...", UsernameUI.text);

        Authenticate(Authtypes.EmailAndPassword);
    }

    public void OnLogoutClicked()
    {
        UnlinkSilentAuth();
        ClearRememberMe();
        AuthType = Authtypes.None;
        SigninPanel.SetActive(true);
        SigninSuccessPanel.SetActive(false);
    }

    private void OnPlayAsGuestClicked()
    {
        StatusText.text = "Logging In As Guess ...";

        Authenticate(Authtypes.Silent);
    }

    private void OnRegisterButtonClicked()
    {
        if(PasswordUI.text != ConfirmPasswordUI.text)
        {
            StatusText.text = "Passwords do not match.";
            return;
        }

        StatusText.text = string.Format("Registerign User {0} ...", UsernameUI.text);

        Authenticate(Authtypes.RegisterPlayfabAccount);
    }

    private void OnLoginWithFaceBookClicked()
    {
        StatusText.text = "Logging into facebook";

        Authenticate(Authtypes.FaceBook);
        //FB.Init(OnFaceBookInit);
    }

    private void OnContinueButtonClicked()
    {
        PlayfabStatsController.SetActive(true);
        Panel.SetActive(false);
        RegisterPanel.SetActive(false);
        SigninPanel.SetActive(false);
        SigninSuccessPanel.SetActive(false);
    }

    private void OnFaceBookInit()
    {
        if (!FB.IsLoggedIn)
        {
            FB.LogInWithReadPermissions(null, OnFaceBookLoggedIn);
        }
    }

    private void OnFaceBookLoggedIn(ILoginResult result)
    {
        if(result == null || string.IsNullOrEmpty(result.Error))
        {
            Debug.Log("Facebook Auth Complete!");
            StatusText.text = "Facebook Auth Complete!";

            PlayFabClientAPI.LoginWithFacebook(new LoginWithFacebookRequest { CreateAccount = true, AccessToken = AccessToken.CurrentAccessToken.TokenString },
                OnPlayfabFacebookAuthComplete, OnPlayFabError);
        }
        else
        {
            Debug.LogError(result.Error);
        }
    }

    private void OnPlayfabFacebookAuthComplete(LoginResult result)
    {
        // Store identity and session
        _playFabId = result.PlayFabId;
        _sessionTicket = result.SessionTicket;
        
            RememberMeId = Guid.NewGuid().ToString();
            AuthType = Authtypes.FaceBook;

            // Fire and forget, but link a custom ID to this PlayFab Account.
            PlayFabClientAPI.LinkCustomID(
        new LinkCustomIDRequest
        {
            CustomId = RememberMeId,
            ForceLink = ForceLink
        },
        null,   // Success callback
        null    // Failure callback
        );

        OnLoginSuccess(result);
    }

    private void OnCancelRegisterButtonClicked()
    {
        UsernameUI.text = string.Empty;
        PasswordUI.text = string.Empty;
        ConfirmPasswordUI.text = string.Empty;

        RegisterPanel.SetActive(false);
        SigninPanel.SetActive(true);
    }

    private void OnLoginSuccess(PlayFab.ClientModels.LoginResult result)
    {
        Debug.LogFormat("Logged In as: {0}", result.PlayFabId);
        Panel.SetActive(false);
        SigninPanel.SetActive(false);
        SigninSuccessText.text = string.Format("You've Successfully Logged in as: {0} ...", result.PlayFabId);
        SigninSuccessPanel.SetActive(true);
        
    }

    private void OnPlayFabError(PlayFabError error)
    {
        //There are more cases which can be caught, below are some
        //of the basic ones.
        switch (error.Error)
        {
            case PlayFabErrorCode.InvalidEmailAddress:
            case PlayFabErrorCode.InvalidPassword:
            case PlayFabErrorCode.InvalidEmailOrPassword:
                StatusText.text = "Invalid Email or Password";
                break;

            case PlayFabErrorCode.AccountNotFound:
                RegisterPanel.SetActive(true);
                SigninPanel.SetActive(false);
                return;
            default:
                StatusText.text = error.GenerateErrorReport();
                break;
        }

        //Also report to debug console, this is optional.
        Debug.Log(error.Error);
        Debug.LogError(error.GenerateErrorReport());
    }

    private void OnDisplayAuthentication()
    {
        Panel.SetActive(true);
    }

    private void Authenticate(Authtypes auth_type)
    {
        AuthType = auth_type;
        Authenticate();
    }

    private void Authenticate()
    {
        Debug.Log(AuthType);
        switch (AuthType)
        {
            case Authtypes.None:
                OnDisplayAuthentication();
                break;
            case Authtypes.Silent:
                SilentlyAuthenticate();
                break;
            case Authtypes.EmailAndPassword:
                AuthenticateEmailPassword();
                break;
            case Authtypes.RegisterPlayfabAccount:
                AddAccountAndPassword();
                break;
            case Authtypes.FaceBook:
                AuthenticateFaceBook();
                break;
            default:
                break;
        }
    }

    private void AuthenticateEmailPassword()
    {
        if(RememberMe && !string.IsNullOrEmpty(RememberMeId))
        {
            PlayFabClientAPI.LoginWithCustomID(
                new LoginWithCustomIDRequest()
                {
                    TitleId = PlayFabSettings.TitleId,
                    CustomId = RememberMeId,
                    CreateAccount = true,
                    InfoRequestParameters = InfoRequestParams
                },

                //Success
                (LoginResult result) =>
                {
                    _playFabId = result.PlayFabId;
                    _sessionTicket = result.SessionTicket;

                    OnLoginSuccess(result);
                },

                //Failure
                (PlayFabError error) =>
                {
                    OnPlayFabError(error);
                }
                );

            return;
        }

        if (string.IsNullOrEmpty(UsernameUI.text) && string.IsNullOrEmpty(PasswordUI.text))
        {
            OnDisplayAuthentication();
            return;
        }

        PlayFabClientAPI.LoginWithEmailAddress(
            new LoginWithEmailAddressRequest()
            {
                TitleId = PlayFabSettings.TitleId,
                Email = UsernameUI.text,
                Password = PasswordUI.text,
                InfoRequestParameters = InfoRequestParams
            },

            // Success
            (LoginResult result) =>
            {
                        // Store identity and session
                        _playFabId = result.PlayFabId;
                _sessionTicket = result.SessionTicket;

                        // If RememberMe is checked, then generate a new Guid for Login with CustomId.
                        if (RememberMe)
                {
                    RememberMeId = Guid.NewGuid().ToString();
                    AuthType = Authtypes.EmailAndPassword;

                            // Fire and forget, but link a custom ID to this PlayFab Account.
                            PlayFabClientAPI.LinkCustomID(
                        new LinkCustomIDRequest
                        {
                            CustomId = RememberMeId,
                            ForceLink = ForceLink
                        },
                        null,   // Success callback
                        null    // Failure callback
                        );
                }
                        
                            OnLoginSuccess(result);
            },

            // Failure
            (PlayFabError error) =>
            {
                 OnPlayFabError(error);
            });
    }

    private void AddAccountAndPassword()
    {
        SilentlyAuthenticate(
            (LoginResult result) =>
            {
                if (result == null)
                {
                    //something went wrong with Silent Authentication, Check the debug console.
                    OnPlayFabError(new PlayFabError()
                    {
                        Error = PlayFabErrorCode.UnknownError,
                        ErrorMessage = "Silent Authentication by Device failed"
                    });
                }
                Debug.Log(result.PlayFabId);
                // Now add our username & password.
                PlayFabClientAPI.AddUsernamePassword(
                    new AddUsernamePasswordRequest()
                    {
                        Username = result.PlayFabId, 
                                Email = UsernameUI.text,
                        Password = PasswordUI.text,
                    },

                    // Success
                    (AddUsernamePasswordResult addResult) =>
                    {
                                    // Store identity and session
                                    _playFabId = result.PlayFabId;
                            _sessionTicket = result.SessionTicket;

                                    // If they opted to be remembered on next login.
                                    if (RememberMe)
                            {
                                        // Generate a new Guid 
                                        RememberMeId = Guid.NewGuid().ToString();

                                        // Fire and forget, but link the custom ID to this PlayFab Account.
                                        PlayFabClientAPI.LinkCustomID(
                                    new LinkCustomIDRequest()
                                    {
                                        CustomId = RememberMeId,
                                        ForceLink = ForceLink
                                    },
                                    null,
                                    null
                                    );
                            }

                                    // Override the auth type to ensure next login is using this auth type.
                                    AuthType = Authtypes.EmailAndPassword;

                                    // Report login result back to subscriber.
                                    OnLoginSuccess(result);
                    },

                    // Failure
                    (PlayFabError error) =>
                    {
                                    //Report error result back to subscriber
                                    OnPlayFabError(error);
                    });
            });
    }

    private void AuthenticateFaceBook()
    {

        if (!string.IsNullOrEmpty(RememberMeId))
        {
            PlayFabClientAPI.LoginWithCustomID(
                new LoginWithCustomIDRequest()
                {
                    TitleId = PlayFabSettings.TitleId,
                    CustomId = RememberMeId,
                    CreateAccount = true,
                    InfoRequestParameters = InfoRequestParams
                },

                //Success
                (LoginResult result) =>
                {
                    _playFabId = result.PlayFabId;
                    _sessionTicket = result.SessionTicket;

                    OnLoginSuccess(result);
                },

                //Failure
                (PlayFabError error) =>
                {
                    OnPlayFabError(error);
                }
                );

            return;
        }

        FB.Init(OnFaceBookInit);

    }

    private void SilentlyAuthenticate(System.Action<LoginResult> callback = null)
    {
        PlayFabClientAPI.LoginWithCustomID(new LoginWithCustomIDRequest()
        {
            TitleId = PlayFabSettings.TitleId,
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
            InfoRequestParameters = InfoRequestParams
        }, (result) =>
        {
            //Store Identity and session
            _playFabId = result.PlayFabId;
            _sessionTicket = result.SessionTicket;

            //check if we want to get this callback directly or send to event subscribers.
            if (callback == null)
            {
                OnLoginSuccess(result);
            }
            else
            {
                //report login result back to the caller
                callback.Invoke(result);
            }
        }, (error) =>
        {
            if (callback == null)
            {
                OnPlayFabError(error);
            }
            else
            {
                //make sure the loop completes, callback with null
                callback.Invoke(null);
                //Output what went wrong to the console.
                Debug.LogError(error.GenerateErrorReport());
            }

        }
            );
    }
}

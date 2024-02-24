using MyServer.Classes;
using Npgsql;
using server1.DB;

namespace MTCG_Test;

[TestFixture]
public class DbCommandsGetUser
{

    private DbCommands userRepository;
    private ConnectionToDB ConnectionToDb = new ConnectionToDB();

    [OneTimeSetUp]
    public void GlobalSetup()
    {
        ConnectionToDb.OpenConnection();
        DatabaseTestCreateEcerything.ResetDb();
        ConnectionToDb.initializeTables();
        DatabaseTestCreateEcerything.RegisterTestUsers();
        DatabaseTestCreateEcerything.CreateSomeCardsForTesting();
        DatabaseTestCreateEcerything.CreateSomePackages();
    }

    [SetUp]
    public void Setup()
    {
        userRepository = new DbCommands(ConnectionToDb);
    }
    [Test]
    public void RegisterUsersReturnsUsersIDs()
    {
        // Arrange
        string testUsername1 = "testUser5";
        string testPassword1 = "TestPassword5";

        // Act
        int result1 = userRepository.registerNewUSer(testUsername1, testPassword1);

        // Assert
        Assert.GreaterOrEqual(result1, 0); // Expecting a positive user_id on successful registration

    }
    [Test]
    public void RegisterUsersShouldFail()
    {
        // Arrange
        string testUsername1 = "testUser3";
        string testPassword1 = "TestPassword1";


        // Act
        int result1 = userRepository.registerNewUSer(testUsername1, testPassword1);


        // Assert
        Assert.AreEqual(-5, result1); // Expecting a positive user_id on successful registration

    }
    [Test, Order(2)]
    public void LoginUserOK()
    {
        // Act
        int result1 = userRepository.authenticationLogin("testUser1", "testPassword1");

        // Assert
        Assert.AreEqual(1, result1, "The authenticationLogin method should return 1 for successful authentication.");
    }
    [Test]
    public void GetUser_WhenUserExists_ReturnsUserObject()
    {
        User? actualUser1 = userRepository.GetUser("testUser3");

        Assert.NotNull(actualUser1);
        Assert.AreEqual(3, actualUser1.user_id);
        Assert.AreEqual("testUser3", actualUser1.Username);
        Assert.AreEqual("testPassword3", actualUser1.Password);
        Assert.AreEqual(20, actualUser1.Coins);


    }
    [Test]
    public void GetUserShouldReturnFalse()
    {
        User? actualUser1 = userRepository.GetUser("pavels");

        Assert.IsNull(actualUser1);
    }
    [Test]
    public void ChangeUsernameOK()
    {
        // Arrange
        string testUsernameBefore = "testUser1";
        string testUsernameToChange = "UserTest1";

        // Act
        int result = userRepository.changeUsername(testUsernameBefore, testUsernameToChange);

        // Assert
        Assert.Greater(result, 0, "The changeUsername method should return a non-negative result for a successful username change.");
    }
    [Test]
    public void ChangeUsernameFalseNoUser()
    {
        // Arrange
        string testUsernameBefore = "pavels";
        string testUsernameToChange = "UserTest1";

        // Act
        int result = userRepository.changeUsername(testUsernameBefore, testUsernameToChange);

        // Assert
        Assert.AreEqual(result, 0);
    }
    [Test]
    public void ChangePasswordOK()
    {
        // Arrange
        string testUsername = "testUser2";
        string testPasswordToChange = "NewPassword";
        // Act
        int result = userRepository.changePassword(testUsername, testPasswordToChange);
        // Assert
        Assert.Greater(result, 0, "The changePassword method should return a non-negative result for a successful password change.");
    }
    [Test]
    public void ChangePasswordShouldFail()
    {
        // Arrange
        string testUsername = "testUser435";
        string testPasswordToChange = "NewPassword";
        // Act
        int result = userRepository.changePassword(testUsername, testPasswordToChange);
        // Assert
        Assert.AreEqual(result, 0);
    }
    [Test]
    public void IsValidUserOK()
    {
        string testUsername = "testUser3";
        string testPassword = "testPassword3";

        // Act
        int result = userRepository.IsValidUser(testUsername, testPassword);

        // Assert
        Assert.AreEqual(0, result, "The IsValidUser method should return 0 for valid credentials.");
    }
    [Test]
    public void IsValidUserShouldReturnMinusOne()
    {
        string testUsername = "testUser3";
        string testPassword = "testPassword123";

        // Act
        int result = userRepository.IsValidUser(testUsername, testPassword);

        // Assert
        Assert.AreEqual(-1, result, "The IsValidUser method should return -1 for wrong password.");
    }
    [Test]
    public void IsValidUserShouldReturnMinusTwo()
    {
        string testUsername = "testUser123";
        string testPassword = "testPassword1";

        // Act
        int result = userRepository.IsValidUser(testUsername, testPassword);

        // Assert
        Assert.AreEqual(-2, result, "The IsValidUser method should return -2 for wrong username.");
    }
    [Test]
    public void buyPackageOK()
    {
        // Arrange
        string testUsername = "testUser1";
        int testPackageId = 1;

        // Act
        int result = userRepository.BuyPackage(testUsername, testPackageId);

        // Assert
        Assert.Greater(result, 0);
    }
    [Test]
    public void buyPackageFailNoMoney()
    {
        // Arrange
        string testUsername = "testUser2";
        int testPackageId = 2;

        // Act
        int result = userRepository.BuyPackage(testUsername, testPackageId);

        // Assert
        Assert.AreEqual(-1, result);
    }
    [Test]
    public void buyPackageFailAlreadyBought()
    {
        // Arrange
        string testUsername = "testUser1";
        int testPackageId = 3;

        // Act
        int result = userRepository.BuyPackage(testUsername, testPackageId);

        // Assert
        Assert.AreEqual(-3, result);
    }
    [Test]
    public void buyPackageFailNoPackage()
    {
        // Arrange
        string testUsername = "testUser1";
        int testPackageId = 999;

        // Act
        int result = userRepository.BuyPackage(testUsername, testPackageId);

        // Assert
        Assert.AreEqual(-5, result);
    }
    [Test]
    public void AddCardToDbOK()
    {
        // Arrange
        string testName = "TestCard";
        string testCategory = "TestCategory";
        string testType = "TestType";
        int testDamage = 10;

        // Act
        int result = userRepository.addCardToDb(testName, testCategory, testType, testDamage);

        // Assert
        Assert.GreaterOrEqual(result, 0, "The addCardToDb method should return a non-negative result for a successful card addition.");
    }
    [Test]
    public void GetCardsFromUser()
    {
        // Arrange
        string testUsername = "testUser3";

        // Act
        List<Card>? result = userRepository.GetCardsFromUser(testUsername);

        // Assert
        Assert.IsNotNull(result, "The result should not be null.");
        Assert.IsNotEmpty(result, "The result should contain cards.");
        // Add more specific assertions based on your expectations
    }
    [Test]
    public void GetCardsFromUserNoUser()
    {
        // Arrange
        string testUsername = "NonexistentUser";

        // Act
        List<Card>? result = userRepository.GetCardsFromUser(testUsername);

        // Assert
        Assert.IsNull(result);
        
    }
    [Test]
    public void GetDeckFromUserOK()
    {
        // Arrange
        string testUsername = "testUser3";

        // Act
        List<Card> result = userRepository.getDeckFromUser(testUsername);

        // Assert
        Assert.IsNotNull(result, "The result should not be null.");
        Assert.IsNotEmpty(result, "The result should contain cards.");
    }
    [Test]
    public void GetDeckFromUserEmpty()
    {
        string testUsername = "testUser2";

        // Act
        List<Card> result = userRepository.getDeckFromUser(testUsername);

        // Assert
        Assert.IsNotNull(result, "The result should not be null.");
        Assert.IsEmpty(result, "The result should contain cards.");
    }
}
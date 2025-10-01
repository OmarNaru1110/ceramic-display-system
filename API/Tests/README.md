# Authentication Unit Tests

This project contains comprehensive unit tests for the authentication functionality of the Ceramic Display System API.

## Test Coverage

### AuthService Tests
- **RegisterAsync**: Tests user registration with valid data, existing username/email, creation failures, and role assignment failures
- **LoginAsync**: Tests login with valid credentials, invalid credentials, users without roles, and email-based login
- **RefreshAccessTokenAsync**: Tests token refresh with valid/invalid/expired tokens
- **RevokeRefreshTokenAsync**: Tests token revocation functionality
- **UpdateUserRolesAsync**: Tests role management functionality

### AuthController Tests
- **RegisterAsync**: Tests controller endpoints for user registration with different roles
- **LoginAsync**: Tests login endpoints with valid/invalid credentials
- **RefreshAccessTokenAsync**: Tests token refresh endpoints
- **RevokeRefreshTokenAsync**: Tests logout/token revocation endpoints

### Model Tests
- **AppUser**: Tests user entity creation, soft delete functionality, and refresh token management
- **RefreshToken**: Tests token expiration, activation status, and property management

### DTO Tests
- **AuthDto, RegisterDto, LoginDto, RefreshTokenDto**: Tests data validation and property assignments

### Integration Tests
- **Database Integration**: Tests Entity Framework operations with in-memory database

## Technologies Used

- **NUnit**: Testing framework
- **Moq**: Mocking framework for dependencies
- **FluentAssertions**: Fluent assertion library for readable tests
- **Entity Framework In-Memory Database**: For integration testing without requiring a real database
- **Microsoft.Extensions.Logging**: For logging in tests
- **AutoMapper**: For object mapping testing

## Running the Tests

### Prerequisites
- .NET 9 SDK
- All dependencies restored (`dotnet restore`)

### Commands

Run all tests:
```bash
dotnet test Tests/Tests.csproj
```

Run with detailed output:
```bash
dotnet test Tests/Tests.csproj --verbosity normal
```

Run specific test class:
```bash
dotnet test Tests/Tests.csproj --filter "ClassName=AuthServiceTests"
```

Run tests with coverage:
```bash
dotnet test Tests/Tests.csproj --collect:"XPlat Code Coverage"
```

## Test Structure

```
Tests/
??? Controllers/
?   ??? AuthControllerTests.cs       # Controller layer tests
??? Services/
?   ??? AuthServiceTests.cs         # Business logic tests
??? Models/
?   ??? AppUserTests.cs             # Entity model tests
?   ??? RefreshTokenTests.cs        # Token model tests
??? DTOs/
?   ??? AuthDtoTests.cs             # Data transfer object tests
??? Integration/
?   ??? DatabaseIntegrationTests.cs # Database integration tests
??? Helpers/
    ??? TestBase.cs                 # Base test class with common setup
    ??? JwtConfig.cs                # JWT configuration for testing
```

## Key Features

### In-Memory Database Testing
All tests use Entity Framework's in-memory database provider, ensuring:
- Fast test execution
- Isolated test data
- No dependency on external databases
- Automatic cleanup between tests

### Comprehensive Mocking
Uses Moq to mock:
- UserManager<AppUser>
- IAuthService
- IConfiguration
- ILogger implementations
- Repository patterns

### Authentication Flow Testing
Tests cover the complete authentication flow:
1. User registration with role assignment
2. Login with JWT token generation
3. Token refresh mechanism
4. Token revocation (logout)
5. Role management

### Error Handling Testing
Validates proper error handling for:
- Invalid credentials
- Duplicate user registration
- Token expiration
- Role assignment failures
- Database operation failures

## Test Data Management

Tests use factory methods in `TestBase.cs` to create consistent test data:
- `CreateTestUser()`: Creates a test user with default properties
- `CreateRegisterDto()`: Creates valid registration data
- `CreateLoginDto()`: Creates valid login credentials

## Assertions

Uses FluentAssertions for readable and expressive test assertions:
```csharp
result.Should().NotBeNull();
result.StatusCode.Should().Be(StatusCodes.OK);
result.Data.AccessToken.Should().NotBeNullOrEmpty();
```

## Notes

- Tests are isolated and can run in parallel
- Each test method has its own database context
- Mock objects are reset between tests
- All async operations are properly awaited
- Tests follow AAA pattern (Arrange, Act, Assert)

## Future Enhancements

Potential additions to the test suite:
- Performance testing for high-load scenarios
- Security testing for JWT token validation
- Integration tests with real database
- End-to-end API testing with TestServer
- Load testing for concurrent user scenarios
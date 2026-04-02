
export const LandingPage = () => {
    return (
        <div className="flex flex-col items-center justify-center min-h-screen bg-gray-100">
            <h1 className="text-4xl font-bold mb-4">Welcome to Our App</h1>
            <p className="text-lg text-gray-600 mb-8">
                Please log in or sign up to continue
            </p>
            <div className="space-x-4">
                <a href="/auth/login" className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
                    Log In
                </a>
                <a href="/auth/register" className="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700">
                    Sign Up
                </a>
            </div>
        </div>
    );
}
import { Outlet, Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { useAuthBootstrap } from "@/features/auth/useAuthBootstrap";
import { useAuthStore } from "@/features/auth/authStore";
import { redirectToLogin } from "@/features/auth/authSession";

function App() {
  useAuthBootstrap();
  const status = useAuthStore((state) => state.status);
  const logout = useAuthStore((state) => state.logout);

  const handleLogout = async () => {
    await logout();
    redirectToLogin();
  };

  return (
    <div className="min-h-screen bg-background">
      <header className="border-b">
        <div className="container mx-auto flex items-center justify-between px-4 py-4">
          <Link to="/" className="text-2xl font-bold">
            Medicaid Application Assistant
          </Link>
          {status === "authenticated" && (
            <Button variant="outline" onClick={handleLogout}>
              Logout
            </Button>
          )}
        </div>
      </header>
      <main className="container mx-auto px-4 py-8">
        <Outlet />
      </main>
    </div>
  );
}

export default App;

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { loginSchema, LoginFormValues } from "./loginSchema";
import { useAuthStore } from "./authStore";

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [formError, setFormError] = useState<string | null>(null);

  const status = useAuthStore((state) => state.status);
  const lastError = useAuthStore((state) => state.lastError);
  const login = useAuthStore((state) => state.login);
  const clearReturnPath = useAuthStore((state) => state.clearReturnPath);
  const returnPath = useAuthStore((state) => state.returnPath);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  });

  const registrationSuccess = Boolean(
    (location.state as { registered?: boolean } | null)?.registered,
  );

  const onSubmit = async (values: LoginFormValues) => {
    setFormError(null);
    const result = await login(values.email, values.password);
    if (!result.ok) {
      setFormError(result.message ?? "Unable to login with those credentials.");
      return;
    }

    const destination = returnPath ?? "/wizard";
    clearReturnPath();
    navigate(destination, { replace: true });
  };

  return (
    <div className="mx-auto max-w-md">
      <Card>
        <CardHeader>
          <CardTitle className="text-2xl">Welcome back</CardTitle>
          <CardDescription>
            Sign in to continue your Medicaid application.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {registrationSuccess && (
            <div
              role="status"
              className="rounded-md border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-800"
            >
              Account created. Please log in.
            </div>
          )}
          {(formError || lastError) && (
            <div
              role="alert"
              aria-live="polite"
              className="rounded-md border border-destructive bg-destructive/10 p-3 text-sm text-destructive"
            >
              {formError ?? lastError}
            </div>
          )}
          <form className="space-y-4" onSubmit={handleSubmit(onSubmit)}>
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                autoComplete="email"
                {...register("email")}
              />
              {errors.email && (
                <p className="text-sm text-destructive" role="alert">
                  {errors.email.message}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Password</Label>
              <Input
                id="password"
                type="password"
                autoComplete="current-password"
                {...register("password")}
              />
              {errors.password && (
                <p className="text-sm text-destructive" role="alert">
                  {errors.password.message}
                </p>
              )}
            </div>
            <Button
              type="submit"
              className="w-full"
              disabled={isSubmitting || status === "authenticating"}
            >
              {status === "authenticating" ? "Signing in..." : "Login"}
            </Button>
          </form>
          <p className="text-sm text-muted-foreground">
            Don&apos;t have an account?{" "}
            <Link
              to="/register"
              className="font-medium text-primary hover:underline"
            >
              Register
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

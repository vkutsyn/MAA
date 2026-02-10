import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Link, useNavigate } from "react-router-dom";
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
import { registerSchema, RegisterFormValues } from "./registerSchema";
import { useAuthStore } from "./authStore";

export function RegisterPage() {
  const navigate = useNavigate();
  const [formError, setFormError] = useState<string | null>(null);

  const status = useAuthStore((state) => state.status);
  const lastError = useAuthStore((state) => state.lastError);
  const registerAccount = useAuthStore((state) => state.register);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
  });

  const onSubmit = async (values: RegisterFormValues) => {
    setFormError(null);
    const result = await registerAccount(
      values.email,
      values.password,
      values.fullName,
    );
    if (!result.ok) {
      setFormError(
        result.message ?? "Unable to create account. Please try again.",
      );
      return;
    }

    navigate("/login", { replace: true, state: { registered: true } });
  };

  return (
    <div className="mx-auto max-w-md">
      <Card>
        <CardHeader>
          <CardTitle className="text-2xl">Create your account</CardTitle>
          <CardDescription>
            Register to save your progress and access the wizard.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
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
              <Label htmlFor="fullName">Full name</Label>
              <Input
                id="fullName"
                type="text"
                autoComplete="name"
                {...register("fullName")}
              />
              {errors.fullName && (
                <p className="text-sm text-destructive" role="alert">
                  {errors.fullName.message}
                </p>
              )}
            </div>
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
                autoComplete="new-password"
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
              {status === "authenticating" ? "Creating account..." : "Register"}
            </Button>
          </form>
          <p className="text-sm text-muted-foreground">
            Already have an account?{" "}
            <Link
              to="/login"
              className="font-medium text-primary hover:underline"
            >
              Login
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

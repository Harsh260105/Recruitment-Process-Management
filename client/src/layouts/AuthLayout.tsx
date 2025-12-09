import { Outlet } from "react-router-dom";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

export const AuthLayout = () => {
  return (
    <div className="min-h-screen bg-muted/30 flex items-center justify-center">
      <div className="mx-auto w-full max-w-5xl p-6">
        {/* Single card that contains two columns: left = info, right = form */}
        <Card className="w-full overflow-hidden shadow-xl">
          <div className="flex flex-col md:flex-row">
            {/* LEFT - Info / branding */}
            <div className="flex flex-1 flex-col gap-6 p-6 bg-muted/10 md:p-8 md:pl-12">
              <div>
                <Button size="lg" className="rounded-full" disabled>
                  ROIMA
                </Button>
              </div>

              <div>
                <h1 className="text-3xl font-bold leading-tight md:text-4xl">
                  Welcome to Roima Intelligence
                </h1>
                <p className="mt-2 text-lg text-muted-foreground">
                  Shaping the future of Manufacturing, Logistics, and Supply Chains
                </p>
              </div>

              <div className="mt-auto hidden md:block">
                {/* Small extra blurb or visual you want on desktop */}
                <p className="text-sm text-muted-foreground">
                  Built for Roima Intelligence — fast, secure, and consistent
                  hiring.
                </p>
              </div>
            </div>

            {/* RIGHT - Form / Outlet */}
            <div className="w-full max-w-md p-6 md:ml-auto md:p-8">
              <Outlet />
            </div>
          </div>
        </Card>
      </div>
      <div className="absolute bottom-4 text-center w-full text-sm text-muted-foreground">
        &copy; {new Date().getFullYear()} Roima Intelligence. All rights reserved.
      </div>
    </div>
  );
};

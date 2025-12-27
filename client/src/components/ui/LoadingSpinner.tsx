import { LoaderIcon } from "lucide-react";

export const LoadingSpinner = () => {
  return (
    <div className="flex items-center justify-center">
      <LoaderIcon className="h-5 w-5 animate-spin text-emerald-500" />
    </div>
  );
};

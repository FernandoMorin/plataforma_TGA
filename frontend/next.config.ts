import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // El navegador solo habla con Next.js; /api/* se reenvía al backend para que la cookie de sesión sea del mismo origen.
  rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${process.env.BACKEND_URL ?? "http://localhost:5015"}/api/:path*`,
      },
    ];
  },
  cacheComponents: true,
  partialPrefetching: true,
  turbopack: {
    rules: {
      "*.css": {
        loaders: ["@tailwindcss/turbopack"],
        as: "*.css",
      },
    },
  },
};

export default nextConfig;

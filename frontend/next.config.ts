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
  // Los PDF llegan hasta 10 MB más el sobre del formulario; el backend es quien rechaza lo que sobra.
  experimental: { proxyClientMaxBodySize: "11mb" },
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

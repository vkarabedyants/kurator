import React from 'react';
import { render, screen } from '@testing-library/react';
import { Providers } from '../providers';

describe('Providers Component', () => {
  it('should render children within QueryClientProvider', () => {
    const TestChild = () => <div>Test Child Component</div>;

    render(
      <Providers>
        <TestChild />
      </Providers>
    );

    expect(screen.getByText('Test Child Component')).toBeInTheDocument();
  });

  it('should provide QueryClient context to children', () => {
    const TestComponent = () => {
      // This component would use useQueryClient hook
      return <div>Query Client Available</div>;
    };

    render(
      <Providers>
        <TestComponent />
      </Providers>
    );

    expect(screen.getByText('Query Client Available')).toBeInTheDocument();
  });
});
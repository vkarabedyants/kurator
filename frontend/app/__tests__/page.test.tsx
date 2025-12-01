import HomePage from '../page';
import { redirect } from 'next/navigation';

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  redirect: jest.fn(),
}));

describe('Home Page', () => {
  it('should redirect to login page', () => {
    HomePage();
    expect(redirect).toHaveBeenCalledWith('/login');
  });
});
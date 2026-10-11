import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { Observable, firstValueFrom, map, of } from 'rxjs';
import { permissionGuard } from './auth.guards';
import { AuthService } from './auth.service';

describe('permissionGuard', () => {
  function runGuard(granted: string[], required: string[]): Promise<boolean | UrlTree> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            hasAll$: (...needed: string[]) => of(new Set(granted)).pipe(map((set) => needed.every((p) => set.has(p)))),
          },
        },
      ],
    });

    const route = { data: { permissions: required } } as unknown as ActivatedRouteSnapshot;
    const result = TestBed.runInInjectionContext(() => permissionGuard(route, {} as RouterStateSnapshot));
    return firstValueFrom(result as Observable<boolean | UrlTree>);
  }

  it('allows navigation when every permission is granted', async () => {
    expect(await runGuard(['roles.view', 'roles.manage'], ['roles.manage'])).toBe(true);
  });

  it('redirects to /forbidden when a permission is missing', async () => {
    const result = await runGuard(['roles.view'], ['roles.manage']);

    const router = TestBed.inject(Router);
    expect(result instanceof UrlTree && router.serializeUrl(result)).toBe('/forbidden');
  });
});

import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-fetch-data',
  templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {
  public forecasts: WeatherForecast[] = [];

  constructor(private http: HttpClient, @Inject('BASE_URL') private baseUrl: string) {
  }

  ngOnInit(): void {
    this.http.get<WeatherForecast[]>(this.baseUrl + 'weatherforecast')
      .subscribe(result => {
          this.forecasts = result;
        }, error =>
          console.error(error)
      );

    this.http.get(this.baseUrl + 'mainservice')
      .subscribe(result => {
          console.dir(result);
        }, error =>
          console.error(error)
      );
  }
}

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}
